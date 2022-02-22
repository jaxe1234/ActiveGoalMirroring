#include "Environment.hpp"
#include "Core.hpp"
#include <iostream>
#include <fstream>
#include <algorithm>
#include <set>
#include <sstream>
#include <stdlib.h>
#include "State.hpp"

void Ingredients::add_ingredients(const std::vector<Recipe>& recipes, const Environment& environment) {
	for (const auto& recipe : recipes) {
		add_ingredients(recipe, environment);
	}
}

void Ingredients::add_ingredients(const Recipe& recipe, const Environment& environment) {
	if (!environment.is_type_stationary(recipe.ingredient1)) {
		add_ingredient(recipe.ingredient1);
	}
	add_ingredient(recipe.ingredient2);
}

void Ingredients::perform_recipes(const std::vector<Recipe>& recipes, const Environment& environment) {
	for (const auto& recipe : recipes) {
		perform_recipe(recipe, environment);
	}
}

void Ingredients::perform_recipe(const Recipe& recipe, const Environment& environment) {
	if (!environment.is_type_stationary(recipe.ingredient1)) {
		--ingredients.at(recipe.ingredient1);
	}
	--ingredients.at(recipe.ingredient2);
	auto it = ingredients.find(recipe.result);
	if (it == ingredients.end()) {
		ingredients.insert({ recipe.result, 1 });
	}
	else {
		++(it->second);
	}
}

bool Ingredients::have_ingredients(const std::vector<Recipe>& recipes, const Environment& environment) const {
	for (const auto& recipe : recipes) {
		if (!have_ingredients(recipe, environment)) {
			return false;
		}
	}
	return true;
}

bool Ingredients::have_ingredients(const Recipe& recipe, const Environment& environment) const {
	return (environment.is_type_stationary(recipe.ingredient1)
		|| get_count(recipe.ingredient1) > 0)
		&& get_count(recipe.ingredient2) > 0;
}

bool Environment::is_inbounds(const Coordinate& coordinate) const {
	return coordinate.first >= 0
		&& coordinate.second >= 0
		&& coordinate.first < width
		&& coordinate.second < height;
}

bool Environment::is_cell_type(const Coordinate& coordinate, const Cell_Type& type) const {
	switch (type) {
		case Cell_Type::WALL: {
			return walls.at(coordinate.first).at(coordinate.second);
		}
		case Cell_Type::CUTTING_STATION: {
			return std::find(cutting_stations.begin(), cutting_stations.end(), coordinate) != cutting_stations.end();
		}
		case Cell_Type::DELIVERY_STATION: {
			return std::find(delivery_stations.begin(), delivery_stations.end(), coordinate) != delivery_stations.end();
		}
	}
	std::cout << "ERROR: Exiting MAC due to invalid cell type.";
	throw std::invalid_argument("Unknown cell type.");
}

bool Environment::is_cell_type(const Coordinate& coordinate, const Direction& direction, const Cell_Type& type) const {
	return is_cell_type(move_noclip(coordinate, direction), type);
}

bool Environment::is_type_stationary(Ingredient ingredient) const {
	switch (ingredient) {
	case Ingredient::CUTTING: return true;
	case Ingredient::DELIVERY: return true;
	default: return false;
	}
}

bool Environment::act(State& state, const Joint_Action& action) const {
	return act(state, action, Print_Level::VERBOSE);
}

// Had to do this method pretty weird, since BD have pretty weird rules
// Any action related to counters (pickup, put down, merge, deliver) is valid as long as it was valid in the original state
// Any action which causes an inter-agent collision is invalid no matter if it was valid in the original state
bool Environment::act(State& state, const Joint_Action& joint_action, Print_Level print_level) const {
	if (contains_collisions(state, joint_action)) {
		return false;
	}
	const State original_state = state;
	bool first = true;
	for (const auto& action : joint_action.actions) {
		if (first) {
			first = false;
			if (!act(state, action, print_level)) {
				return false;
			}
		}
		else {
			State state_copy = original_state;
			if (!act(state_copy, action, print_level)) {
				return false;
			}
			act(state, action, print_level);
		}
	}
	return true;
}

bool Environment::act(State& state, const Action& action) const {
	return act(state, action, Print_Level::VERBOSE);
}

bool Environment::act(State& state, const Action& action, Print_Level print_level) const {
	if (action.direction == Direction::NONE) {
		return true;
	}

	auto& agent = state.agents.at(action.agent.id);
	Coordinate old_position = agent.coordinate;
	Coordinate new_position = move_noclip(old_position, action.direction);

	auto item_old_position = agent.item;
	auto item_new_position = state.get_ingredient_at_position(new_position);

	// Simple move
	if (!is_cell_type(new_position, Cell_Type::WALL)) {
		state.agents.at(action.agent.id).move_to(new_position);
		PRINT(Print_Category::ENVIRONMENT, print_level, std::string("Moved ") + static_cast<char>(action.direction) + "\n");
		return true;

		// Goal condition
	}
	else if (is_cell_type(new_position, Cell_Type::DELIVERY_STATION)) {
		if (item_old_position.has_value()) {
			auto recipe = get_recipe(Ingredient::DELIVERY, item_old_position.value());
			if (recipe.has_value()) {
				agent.clear_item();
				state.add_goal_item(new_position, recipe.value());
				PRINT(Print_Category::ENVIRONMENT, print_level, std::string("goal ingredient delivered ") + static_cast<char>(recipe.value()) + "\n");
				return true;
			}
		}
		// Turns out it is always allowed to hump the delivery station
		return true;

		// Combine
	}
	else if (item_new_position.has_value() && item_old_position.has_value()) {
		auto recipe = get_recipe(item_old_position.value(), item_new_position.value());
		if (recipe.has_value()) {
			state.remove(new_position);
			agent.clear_item();
			agent.set_item(recipe.value());
			PRINT(Print_Category::ENVIRONMENT, print_level, std::string("Combine ") + static_cast<char>(recipe.value()) + "\n");
			return true;
		}
		else {
			auto recipe_reverse = get_recipe(item_new_position.value(), item_old_position.value());
			if (recipe_reverse.has_value()) {
				state.remove(new_position);
				agent.clear_item();
				agent.set_item(recipe_reverse.value());
				PRINT(Print_Category::ENVIRONMENT, print_level, std::string("Combine ") + static_cast<char>(recipe_reverse.value()) + "\n");
				return true;
			}
		}

		// Chop chop / place
	}
	else if (is_cell_type(new_position, Cell_Type::CUTTING_STATION) && !item_new_position.has_value() && item_old_position.has_value()) {
		auto recipe = get_recipe(Ingredient::CUTTING, item_old_position.value());
		if (recipe.has_value()) {
			agent.clear_item();
			agent.set_item(recipe.value());
			PRINT(Print_Category::ENVIRONMENT, print_level, std::string("chop chop ") + static_cast<char>(recipe.value()) + "\n");
			return true;
		}
		else {
			agent.clear_item();
			state.add(new_position, item_old_position.value());
			PRINT(Print_Category::ENVIRONMENT, print_level, std::string("pickup") + "\n");
			return true;
		}


		// Place
	}
	else if (item_old_position.has_value()) {
		agent.clear_item();
		state.add(new_position, item_old_position.value());
		PRINT(Print_Category::ENVIRONMENT, print_level, std::string("place ") + static_cast<char>(item_old_position.value()) + "\n");
		return true;

		// Pickup
	}
	else if (item_new_position.has_value()) {
		state.remove(new_position);
		agent.set_item(item_new_position.value());
		PRINT(Print_Category::ENVIRONMENT, print_level, std::string("pickup ") + static_cast<char>(item_new_position.value()) + "\n");
		return true;
	}
	// They simply decided to hump a wall
	PRINT(Print_Category::ENVIRONMENT, print_level, std::string("Nothing happened") + "\n");
	return false;
}

// This collision check is basically a one to one from the BD, but I believe it allows two agents to collide if one is no'opping, need to check this
// UPDATE: the original code did allow collision with stationary agents, changed it to disallow this even though I believe that BD allows it
// To clarify, it seem the BD environment is over liberal with what it allows, the real restrictions in what actions the BD planner considers legal
bool Environment::contains_collisions(const State& state, const Joint_Action& joint_action) const {
	std::vector<Coordinate> current_coordinates;
	std::vector<Coordinate> next_coordinates;
	std::vector<Coordinate> action_coordinates;

	std::set<size_t> cancelled_agents;

	for (size_t agent = 0; agent < joint_action.actions.size(); ++agent) {
		current_coordinates.push_back(state.agents.at(agent).coordinate);
		next_coordinates.push_back(move(state.agents.at(agent).coordinate, joint_action.actions.at(agent).direction));
		action_coordinates.push_back(move_noclip(state.agents.at(agent).coordinate, joint_action.actions.at(agent).direction));
	}

	for (size_t agent1 = 0; agent1 < joint_action.actions.size(); ++agent1) {
		for (size_t agent2 = agent1 + 1; agent2 < joint_action.actions.size(); ++agent2) {
			if (next_coordinates.at(agent1) == next_coordinates.at(agent2)) {

				// Agent1 still, agent2 invalid
				if (current_coordinates.at(agent1) == next_coordinates.at(agent1)) {
					cancelled_agents.insert(agent2);

					// Agent2 still, agent1 invalid
				}
				else if (current_coordinates.at(agent2) == next_coordinates.at(agent2)) {
					cancelled_agents.insert(agent1);
				}
				else {
					cancelled_agents.insert(agent1);
					cancelled_agents.insert(agent2);
				}
				// Swap (invalid)
			}
			else if (current_coordinates.at(agent1) == next_coordinates.at(agent2)
				|| current_coordinates.at(agent2) == next_coordinates.at(agent1)) {
				cancelled_agents.insert(agent1);
				cancelled_agents.insert(agent2);

			}
		}
	}
	return !cancelled_agents.empty();
}

std::vector<Action> Environment::get_actions(Agent_Id agent) const {
	return { {Direction::UP, agent}, {Direction::RIGHT, agent}, {Direction::DOWN, agent}, {Direction::LEFT, agent}, {Direction::NONE, agent} };
}

std::vector<Joint_Action> Environment::get_joint_actions(const Agent_Combination& agents) const {
	std::vector<std::vector<Action>> single_actions;
	std::vector<size_t> counters;
	for (size_t agent = 0; agent < number_of_agents; ++agent) {
		if (agents.contains({ agent })) {
			single_actions.push_back(get_actions({ agent }));
		}
		else {
			single_actions.push_back({ { Direction::NONE, agent } });
		}
		counters.emplace_back(0);
	}

	std::vector<Joint_Action> joint_actions;

	bool done = false;
	while (!done) {
		std::vector<Action> actions;
		for (size_t i = 0; i < counters.size(); i++) {
			actions.push_back(single_actions[i][counters[i]]);
		}
		joint_actions.push_back(std::move(actions));

		// Advance indices
		size_t index = 0;
		while (!done && ++counters.at(index) >= single_actions.at(index).size()) {
			counters.at(index++) = 0;
			done = index >= counters.size();
		}
	}
	return joint_actions;
}

State Environment::load(const std::string& path) {
	reset();
	std::ifstream file;
	file.open(path);
	std::string line;

	if (!file) {
		std::cout << "ERROR: Exiting MAC due to unknown file.";
		throw std::invalid_argument("Unknown file: " + path);
	}

	State state;

	size_t load_status = 0;
	size_t line_counter = 0;
	while (std::getline(file, line)) {
		if (line_counter == 0) {
			width = line.size();
		}

		if (line.empty()) {
			if (load_status == 0) {
				height = line_counter;
			}
			++load_status;
			continue;
		}

		// Level file as defined by BD paper is split in 3 sections
		switch (load_status) {
		case 0: {
			load_map_line(state, line_counter, line, width);
			break;
		}
		case 1: {
			goal_names.push_back(line);
			goal_ingredients.add_ingredient(goal_name_to_ingredient(line));
			break;
		}
		case 2: {
			int x = atoi(&line[0]);
			int y = atoi(&line[2]);
			agents_initial_positions.push_back({ x, y });
			break;
		}
		}
		std::stringstream buffer;
		buffer << line_counter << ": " << line << std::endl;
		PRINT(Print_Category::ENVIRONMENT, Print_Level::DEBUG, buffer.str());
		++line_counter;
	}

	for (size_t agent = 0; agent < number_of_agents; ++agent) {
		state.agents.push_back(agents_initial_positions.at(agent));
	}

	calculate_recipes();

	flip_walls_array();
	file.close();
	return state;
}

void Environment::load_map_line(State& state, size_t& line_counter, const std::string& line, size_t width) {

	std::vector<bool> wall_line;
	wall_line.reserve(width);
	size_t index_counter = 0;
	for (const auto& c : line) {

		// Environment related
		if (c != ' ') wall_line.push_back(true);
		else wall_line.push_back(false);
		if (c == static_cast<char>(Cell_Type::CUTTING_STATION)) cutting_stations.push_back({ index_counter, line_counter });
		if (c == static_cast<char>(Cell_Type::DELIVERY_STATION)) delivery_stations.push_back({ index_counter, line_counter });

		// State related
		const static std::vector<Ingredient> state_ingredients{
			Ingredient::TOMATO,				Ingredient::CHOPPED_TOMATO, Ingredient::LETTUCE,
			Ingredient::CHOPPED_LETTUCE, 	Ingredient::PLATE,			Ingredient::PLATED_TOMATO,
			Ingredient::PLATED_LETTUCE,		Ingredient::PLATED_SALAD, 	Ingredient::DELIVERED_TOMATO,
			Ingredient::DELIVERED_LETTUCE,	Ingredient::DELIVERED_SALAD,Ingredient::SALAD };

		for (const auto& ingredient : state_ingredients) {
			if (c == static_cast<char>(ingredient)) state.items.insert({ {index_counter, line_counter}, ingredient });
		}

		++index_counter;
	}
	walls.push_back(wall_line);
}

void Environment::flip_walls_array() {

	// Flips walls array so the first coordinate is x, not y
	auto walls_copy = walls;
	for (size_t y = 0; y < walls_copy.size(); ++y) {
		for (size_t x = 0; x < walls_copy.at(0).size(); ++x) {
			walls.at(x).at(y) = walls_copy.at(y).at(x);
		}
	}
}

void Environment::print_state() const {
	State state;
	print_state(state);
}
void Environment::print_state(const State& state) const {
	std::string buffer;
	for (size_t y = 0; y < walls.size(); ++y) {
		for (size_t x = 0; x < walls.at(0).size(); ++x) {
			auto agent_it = std::find_if(state.agents.begin(), state.agents.end(), [x, y](Agent agent)->bool {return agent.coordinate == Coordinate{ x, y }; });

			if (state.items.find({ x, y }) != state.items.end()) {
				buffer += static_cast<char>(state.items.at({ x, y }));

			}
			else if (agent_it != state.agents.end()) {
				if (agent_it->item.has_value()) {
					buffer += static_cast<char>(agent_it->item.value());
				}
				else {
					char buf[2];
					sprintf(buf, "%d", std::distance(state.agents.begin(), agent_it));
					buffer += buf[0];
				}

			}
			else if (std::find(cutting_stations.begin(), cutting_stations.end(), Coordinate{ x, y }) != cutting_stations.end()) {
				buffer += static_cast<char>(Cell_Type::CUTTING_STATION);

			}
			else if (std::find(delivery_stations.begin(), delivery_stations.end(), Coordinate{ x, y }) != delivery_stations.end()) {
				buffer += static_cast<char>(Cell_Type::DELIVERY_STATION);

			}
			else if (walls.at(x).at(y)) {
				buffer += static_cast<char>(Cell_Type::WALL);

			}
			else {
				buffer += ' ';
			}

		}
		buffer += '\n';
	}
	buffer += '\n';
	PRINT(Print_Category::ENVIRONMENT, Print_Level::DEBUG, buffer);
}

void Environment::play(State& state) const {
	bool done = false;
	Agent_Id agent = 0;
	std::cout << "type {w, a, s, d} to move single agent, {w, a, s, d, n}* to move multiple agents, {0-9} to switch single agent, and {q} to quit" << std::endl;
	for (std::string line; std::getline(std::cin, line) && !done;) {
		char c = line[0];
		if (c >= '0' && c <= '9') {
			agent = { static_cast<size_t>(atoi(&c)) };
			PRINT(Print_Category::ENVIRONMENT, Print_Level::VERBOSE, std::string("Switcharoo ") + c);
		}
		else {

			std::vector<Action> actions;
			for (size_t i = 0; i < number_of_agents; ++i) {
				actions.push_back({ Direction::NONE, i });
			}

			for (size_t i = 0; i < line.size(); ++i) {
				Direction dir = Direction::NONE;
				switch (line.at(i)) {
				case 'q': done = true; break;
				case 'w': dir = Direction::UP; break;
				case 'a': dir = Direction::LEFT; break;
				case 's': dir = Direction::DOWN; break;
				case 'd': dir = Direction::RIGHT; break;
				}
				if (line.size() > 1) {
					actions.at(i).direction = dir;
				}
				else {
					actions.at(agent.id).direction = dir;
				}
			}
			act(state, { actions });
		}

		print_state(state);
	}
}

Ingredient Environment::goal_name_to_ingredient(const std::string& name) const {
	if (name == "Salad") {
		return Ingredient::DELIVERED_SALAD;
	}
	else if (name == "SimpleTomato") {
		return Ingredient::DELIVERED_TOMATO;
	}
	else if (name == "SimpleLettuce") {
		return Ingredient::DELIVERED_LETTUCE;
	}

	std::cout << "ERROR: Exiting MAC due to invalid goal.";
    throw std::invalid_argument("Unknown goal ingredient: " + name);
}

void Environment::load_recipes() {
	static const std::map<std::pair<Ingredient, Ingredient>, Ingredient> recipes_raw = {
	{ {Ingredient::CUTTING, Ingredient::TOMATO}, Ingredient::CHOPPED_TOMATO},
	{ {Ingredient::CUTTING, Ingredient::LETTUCE}, Ingredient::CHOPPED_LETTUCE},

	{ {Ingredient::PLATE, Ingredient::CHOPPED_LETTUCE}, Ingredient::PLATED_LETTUCE},
	{ {Ingredient::PLATE, Ingredient::CHOPPED_TOMATO}, Ingredient::PLATED_TOMATO},
	{ {Ingredient::PLATE, Ingredient::SALAD}, Ingredient::PLATED_SALAD},

	{ {Ingredient::CHOPPED_LETTUCE, Ingredient::CHOPPED_TOMATO}, Ingredient::SALAD},
	{ {Ingredient::PLATED_LETTUCE, Ingredient::CHOPPED_TOMATO}, Ingredient::PLATED_SALAD},
	{ {Ingredient::PLATED_TOMATO, Ingredient::CHOPPED_LETTUCE}, Ingredient::PLATED_SALAD},

	{ {Ingredient::DELIVERY, Ingredient::PLATED_SALAD}, Ingredient::DELIVERED_SALAD},
	{ {Ingredient::DELIVERY, Ingredient::PLATED_TOMATO}, Ingredient::DELIVERED_TOMATO},
	{ {Ingredient::DELIVERY, Ingredient::PLATED_LETTUCE}, Ingredient::DELIVERED_LETTUCE},
	};
	all_recipes = {};
	recipes_map = {};
	for (const auto& recipe : recipes_raw) {
		all_recipes.push_back({ recipe.first.first, recipe.first.second, recipe.second });
		recipes_map.insert({ {recipe.first.first, recipe.first.second}, recipe.second });
	}
}

// Not a great way to define recipes, but functional for now
std::optional<Ingredient> Environment::get_recipe(Ingredient ingredient1, Ingredient ingredient2) const {
	auto recipe_it = recipes_map.find({ ingredient1, ingredient2 });
	if (recipe_it != recipes_map.end()) {
		return recipe_it->second;
	}
	else {
		return {};
	}
}


std::vector<Recipe> Environment::get_possible_recipes(const State& state) const {
	std::vector<Recipe> possible_recipes;
	auto ingredients_count_in = state.get_ingredients_count();

	for (const auto& recipe : goal_related_recipes) {

		if ((state.contains_item(recipe.ingredient1)
			|| recipe.ingredient1 == Ingredient::CUTTING
			|| recipe.ingredient1 == Ingredient::DELIVERY)
			&& state.contains_item(recipe.ingredient2)) {

			auto ingredients_count = ingredients_count_in;

			if (does_recipe_lead_to_goal(ingredients_count, recipe)) {
				possible_recipes.push_back(recipe);
			}
		}
	}
	return possible_recipes;
}

bool Environment::does_recipe_lead_to_goal(const Ingredients& ingredients_count_in,
	const Recipe& recipe_in) const {

	// Calculate updated ingredients count
	auto ingredients_count = ingredients_count_in;
	ingredients_count.perform_recipe(recipe_in, *this);

	// Recurse on recipes
	return do_ingredients_lead_to_goal(ingredients_count);
}

bool Environment::do_ingredients_lead_to_goal(const Ingredients& ingredients) const {

	// Check goal condition
	if (goal_ingredients <= ingredients) {
		return true;
	}

	for (const auto& recipe : all_recipes) {
		Ingredients recipe_ingredients;
		recipe_ingredients.add_ingredients(recipe, *this);
		if (recipe_ingredients > ingredients) {
			continue;
		}
		if (does_recipe_lead_to_goal(ingredients, recipe)) {
			return true;
		}
	}
	return false;
}

const std::vector<Recipe>& Environment::get_all_recipes() const {
	return goal_related_recipes;
}

bool Environment::is_done(const State& state) const {
	auto state_ingredients = state.get_ingredients_count();
	return goal_ingredients <= state_ingredients;
}

Coordinate Environment::move(const Coordinate& coordinate, Direction direction) const {
	Coordinate new_coordinate;
	switch (direction) {
	case Direction::UP:		new_coordinate = { coordinate.first, coordinate.second - 1 }; break;
	case Direction::RIGHT:	new_coordinate = { coordinate.first + 1, coordinate.second }; break;
	case Direction::DOWN:	new_coordinate = { coordinate.first, coordinate.second + 1 }; break;
	case Direction::LEFT:	new_coordinate = { coordinate.first - 1, coordinate.second }; break;
	default: return coordinate;
	}
	if (walls.at(new_coordinate.first).at(new_coordinate.second)) {
		return coordinate;
	}
	else {
		return new_coordinate;
	}
}

Coordinate Environment::move_noclip(const Coordinate& coordinate, Direction direction) const {
	switch (direction) {
	case Direction::UP:		return { coordinate.first, coordinate.second - 1 };
	case Direction::RIGHT:	return { coordinate.first + 1, coordinate.second };
	case Direction::DOWN:	return { coordinate.first, coordinate.second + 1 };
	case Direction::LEFT:	return { coordinate.first - 1, coordinate.second };
	default: return coordinate;
	}
}

size_t Environment::get_number_of_agents() const {
	return number_of_agents;
}


Joint_Action Environment::convert_to_joint_action(const Action& action, Agent_Id agent) const {
	std::vector<Action> actions;
	for (size_t i = 0; i < number_of_agents; ++i) {
		if (i == agent.id) {
			actions.push_back(action);
		}
		else {
			actions.push_back({ Direction::NONE, {i} });
		}
	}
	return { actions };
}
std::vector<Location> Environment::get_locations(const State& state, Ingredient ingredient) const {
	std::vector<Location> result;
	switch (ingredient) {
	case Ingredient::CUTTING: {
		for (auto& coord : cutting_stations) result.push_back({ coord, coord, false });
		return result;
	}
	case Ingredient::DELIVERY: {
		for (auto& coord : delivery_stations) result.push_back({ coord, coord, false });
		return result;
	}
	default: return state.get_locations(ingredient);
	}
}

// Assumes ingredient is non-stationary
std::vector<Location> Environment::get_non_wall_locations(const State& state, Ingredient ingredient) const {
	return state.get_non_wall_locations(ingredient, *this);
}


std::vector<Coordinate> Environment::get_coordinates(const State& state, Ingredient ingredient, bool include_agent_holding) const {
	switch (ingredient) {
	case Ingredient::CUTTING: return cutting_stations;
	case Ingredient::DELIVERY: return delivery_stations;
	default: return state.get_coordinates(ingredient, include_agent_holding);
	}
}

std::vector<Coordinate> Environment::get_recipe_locations(const State& state, Ingredient ingredient) const {
	throw std::runtime_error("");
}

void Environment::reset() {
	goal_names.clear();
	goal_ingredients.clear();
	agents_initial_positions.clear();

	walls.clear();
	cutting_stations.clear();
	delivery_stations.clear();
	goal_related_recipes.clear();
}


void Environment::calculate_recipes() {

	std::set<Ingredient> ingredients = goal_ingredients.get_types();
	auto ingredients_size = EMPTY_VAL;
	while (ingredients_size != ingredients.size()) {
		ingredients_size = ingredients.size();
		for (const auto& recipe : all_recipes) {
			if (ingredients.find(recipe.result) != ingredients.end()) {
				ingredients.insert(recipe.ingredient1);
				ingredients.insert(recipe.ingredient2);
			}
		}
	}

	std::vector<Recipe> result;
	for (const auto& recipe : all_recipes) {
		if (ingredients.find(recipe.ingredient1) != ingredients.end()
			&& ingredients.find(recipe.ingredient2) != ingredients.end()) {
			result.push_back(recipe);
		}
	}
	goal_related_recipes = result;
}


size_t Environment::get_width() const {
	return width;
}
size_t Environment::get_height() const {
	return height;
}

std::vector<Coordinate> Environment::get_neighbours(Coordinate location) const {
	return {
		{location.first, location.second - 1},
		{location.first + 1, location.second},
		{location.first, location.second + 1},
		{location.first - 1, location.second} };
}

bool Environment::is_action_none_nav(const Coordinate& coordinate, const Action& action) const {
	return is_cell_type(move_noclip(coordinate, action.direction), Cell_Type::WALL);
}


Direction Environment::get_direction(const Coordinate& source, const Coordinate& dest) const {
	switch (((int)dest.first) - source.first) {
	case -1:return Direction::LEFT;
	case 1:return Direction::RIGHT;
	}
	switch (((int)dest.second) - source.second) {
	case -1:return Direction::UP;
	case 1:return Direction::DOWN;
	}
	return Direction::NONE;
}