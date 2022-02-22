#pragma once

#include "Environment.hpp"
#include "Core.hpp"
#include "Search.hpp"
#include "State.hpp"
#include "Recogniser.hpp"
#include "Planner.hpp"

#include <vector>
#include <set>
#include <deque>

struct Action_Path {
	Action_Path(std::vector<Joint_Action> joint_actions,
		const Goal goal,
		const State& state_in,
		const Environment& environment)
		: joint_actions(joint_actions), recipe(goal.recipe), agents(goal.agents),
		handoff_agent(goal.handoff_agent),
		first_action(EMPTY_VAL), last_action(EMPTY_VAL) {

		if (joint_actions.empty() || handoff_agent == EMPTY_VAL) {
			return;
		}
		auto initial_coordinate = state_in.get_agent(handoff_agent).coordinate;

		// Stop at first action which interacts with a wall
		first_action = 0;
		auto coordinate = initial_coordinate;
		while (true) {
			auto direction = joint_actions.at(first_action).get_action(handoff_agent).direction;
			auto coordinate_noclip = environment.move_noclip(coordinate, direction);

			// New useful definition
			if (environment.is_cell_type(coordinate_noclip, Cell_Type::WALL)) {
				auto item = state_in.get_ingredient_at_position(coordinate_noclip);
				auto agent_item = state_in.get_agent(handoff_agent).item;
				if (item.has_value()
					&& (item.value() == goal.recipe.ingredient1
						|| item.value() == goal.recipe.ingredient2)) {
					break;
				}
				else if (agent_item.has_value()
					&& (agent_item.value() == goal.recipe.ingredient1
						|| agent_item.value() == goal.recipe.ingredient2)) {
					break;
				}
			}

			if (first_action == joint_actions.size() - 1) {
				first_action = EMPTY_VAL;
				break;
			}
			++first_action;
			coordinate = environment.move(coordinate, direction);
		}

		// Note last action which interacts with a wall 
		// (note wall check is using noclip, but coordinate tracking is using regular move)
		coordinate = initial_coordinate;
		last_action = EMPTY_VAL;
		size_t action_counter = 0;
		auto state = state_in;
		while (action_counter < joint_actions.size()) {
			assert(environment.act(state, joint_actions.at(action_counter)));
			const auto& direction = joint_actions.at(action_counter).get_action(handoff_agent).direction;

			// New useful definition
			if (environment.is_cell_type(environment.move_noclip(coordinate, direction), Cell_Type::WALL)) {
				auto item = state.get_ingredient_at_position(environment.move_noclip(coordinate, direction));
				auto agent_item = state.get_agent(handoff_agent).item;
				if (item.has_value()
					&& (item.value() == goal.recipe.ingredient1
						|| item.value() == goal.recipe.ingredient2)) {
					last_action = action_counter;
				}
				else if (agent_item.has_value()
					&& (agent_item.value() == goal.recipe.ingredient1
						|| agent_item.value() == goal.recipe.ingredient2)) {
					last_action = action_counter;;
				}
			}

			coordinate = environment.move(coordinate, direction);
			++action_counter;
		}
	}

	bool operator<(const Action_Path& other) const {
		if (joint_actions.size() != other.joint_actions.size()) return joint_actions.size() < other.joint_actions.size();
		if (agents.size() != other.agents.size()) return agents.size() < other.agents.size();
		if (recipe != other.recipe) return recipe < other.recipe;
		if (agents != other.agents) return agents < other.agents;
	}

	size_t size() const {
		return joint_actions.size();
	}

	size_t length() const {
		return joint_actions.size();
	}

	bool empty() const {
		return joint_actions.empty();
	}

	bool contains_useful_action() const {
		return last_action != EMPTY_VAL;
	}

	bool has_useful_action(const Agent_Id& agent, const State& state, const Environment& environment) const {

		auto coordinate = state.get_agent(agent).coordinate;
		size_t action_counter = 0;
		while (action_counter < joint_actions.size()) {
			coordinate = environment.move_noclip(coordinate, joint_actions.at(action_counter).get_action(agent).direction);
			if (environment.is_cell_type(coordinate, Cell_Type::WALL)) {
				return true;
			}
			++action_counter;
		}
		return false;
	}

	size_t get_first_non_trivial_index(Agent_Id agent) const {
		size_t index = 0;
		for (const auto& action : joint_actions) {
			if (action.is_not_none(agent)) {
				return index;
			}
			++index;
		}
		return EMPTY_VAL;
	}

	Action get_next_action(Agent_Id agent) const {
		if (joint_actions.empty()) return { Direction::NONE, agent };
		return joint_actions.at(0).get_action(agent);
	}

	std::string first_action_string() {
		return first_action == EMPTY_VAL ? "X" : std::to_string(first_action);
	}

	std::string last_action_string() {
		return last_action == EMPTY_VAL ? "X" : std::to_string(last_action);
	}

	std::vector<Joint_Action> joint_actions;
	Recipe recipe;
	Agent_Combination agents;
	size_t first_action;	// First useful action by handoff_agent
	size_t last_action;		// Last useful action by handoff_agent

	Agent_Id handoff_agent;
};

struct Paths {
	Paths() : handoff_map(), handoff_paths() {}

	Paths(const Paths& other) {
		for (const auto& [goal, path_ptr] : other.handoff_map) {
			this->insert(goal, *path_ptr);
		}
	}

	void insert(const Goal& goal, const Action_Path& path) {
		handoff_paths.push_back(path);
		handoff_map.insert({ goal, &handoff_paths.back() });
	}

	void insert(const std::vector<Joint_Action>& actions, const Goal& goal,
		const State& state, const Environment& environment) {

		handoff_paths.push_back({ actions, goal, state, environment });
		Action_Path* path_ptr = &handoff_paths.back();
		handoff_map.insert({ goal, path_ptr });

	}

	void update(const std::vector<Joint_Action>& actions,
		const Goal& goal, const State& state, const Environment& environment) {

		auto it = handoff_map.find(goal);
		if (it == handoff_map.end()) {
			insert(actions, goal, state, environment);
		}
		else {
			(*it->second) = Action_Path(actions, goal, state, environment);
		}
	}

	const std::map<Goal, Action_Path*>& get_handoff() const {
		return handoff_map;
	}

	std::optional<const Action_Path*> get_handoff(const Goal& goal) const {
		auto it = handoff_map.find(goal);
		if (it == handoff_map.end()) {
			return {};
		}
		else {
			return it->second;
		}
	}

	bool empty() const {
		return handoff_paths.empty();
	}

private:
	std::deque<Action_Path> handoff_paths;
	std::map<Goal, Action_Path*> handoff_map;
};

struct Permutations {
	Permutations(size_t max_size) : permutations(max_size, std::vector<Agent_Combination>()) {}

	void insert(const std::vector<Agent_Id>& agents) {
		permutations.at(agents.size() - 1).push_back(Agent_Combination(agents));
	}
	void insert(const std::vector<std::vector<Agent_Id>>& agents_list) {
		for (const auto& agents : agents_list) {
			permutations.at(agents.size() - 1).push_back(Agent_Combination(agents));
		}
	}
	void insert(const Agent_Combination& agents) {
		permutations.at(agents.size() - 1).push_back(agents);
	}

	const std::vector<Agent_Combination>& get(size_t size) {
		return permutations.at(size - 1);
	}

	std::vector<std::vector<Agent_Combination>> permutations;
};

struct Collaboration_Info {
	Collaboration_Info() : length(EMPTY_VAL),
		next_action(), value(EMPTY_VAL), goals(), chosen_goal(), path_length(EMPTY_VAL) {}

	Collaboration_Info(size_t length, const Goals& goals, const Action& next_action,
		Goal chosen_goal, size_t path_length)
		: length(length), goals(goals), next_action(next_action),
		chosen_goal(chosen_goal), path_length(path_length), value(EMPTY_VAL) {}

	std::string to_string() const {
		if (goals.has_same_agents()) {
			std::string result;
			for (const auto& goal : goals.get_iterable()) {
				result += goal.recipe.result_char();
			}
			result += ":" + goals.get_agents().to_string_raw()
				+ ":" + goals.get_handoff_string();

			return result;
		}
		else {
			return "String todo - planner_mac.hpp";
		}
	}

	bool has_value() const {
		return length != EMPTY_VAL;
	}

	bool is_only_agent(Agent_Id agent) const {
		const auto& agents = goals.get_agents();
		return agents.size() == 1 && *agents.begin() == agent;
	}

	size_t goals_size() const {
		return goals.size();
	}

	size_t agents_size() const {
		return get_agents().size();
	}

	const Agent_Combination& get_agents() const {
		return goals.get_agents();
	}

	const Goals& get_goals() const {
		return goals;
	}

	const std::vector<Goal>& get_goals_iterable() const {
		return goals.get_iterable();
	}

	Action next_action;
	size_t length;
	float value;
	Goal chosen_goal;
	size_t path_length;

private:
	Goals goals;
};

struct Recipe_Solution {
	Agent_Combination agents;
	size_t action_count;
	bool operator>(const Action_Path& other) const {
		if (action_count != other.joint_actions.size()) return action_count > other.joint_actions.size();
		return agents.size() > other.agents.size();
	}
};

struct Reachables {
	Reachables(size_t width, size_t height) : data((width* height), false), width(width) {}
	void set(Coordinate location, bool value) {
		data.at(location.second * width + location.first) = value;
	}
	bool get(Coordinate location) const {
		return data.at(location.second * width + location.first);
	}
	std::vector<bool> data;
	size_t width;
};

struct Solution_History {
	Solution_History() : history() {}
	std::vector<size_t> history;
	void add(size_t solution_length, size_t time_step) {
		while (history.size() < time_step) {
			history.push_back(0);
		}
		history.push_back(solution_length);
	}
	size_t get(size_t i) const {
		if (history.size() < i + 1) {
			return 0;
		}
		else {
			return history.at(i);
		}
	}
};

struct Goal_Agents {
	Agent_Combination get(Goal goal) {
		return data[goal];
	}
	void add(Goal goal, Agent_Combination agents) {
		data[goal].add(agents);
	}
	void add(Goal goal, Agent_Id agent) {
		data[goal].add(agent);
	}
	bool empty(Goal goal) const {
		return data.find(goal) == data.end();
	}
	void set_chosen_goal(Goal goal) {
		chosen_goal = goal;
	}
	Goal get_chosen_goal() {
		return chosen_goal;
	}
	std::map<Goal, Agent_Combination> data;
	Goal chosen_goal;
};

class Planner_Mac : public Planner_Impl {


public:
	Planner_Mac(Environment environment, Agent_Id agent, const State& initial_state, size_t seed = 0);
	virtual Action get_next_action(const State& state, bool print_state) override;

private:

	std::map<Goals, float>					calculate_goal_values(std::vector<Collaboration_Info>& infos);
	std::vector<Collaboration_Info>			calculate_infos(const Paths& paths, const std::vector<Recipe>& recipes_in,
		const State& state);
	std::vector<Collaboration_Info>			calculate_probable_multi_goals(const std::vector<Collaboration_Info>& infos,
		const std::map<Goals, float>& goal_values, const State& state);
	std::pair<std::vector<Joint_Action>,
		Goal_Agents>						get_actions_from_permutation(
			const Goals& goals, const Paths& paths, const State& state);
	std::pair<std::vector<Action>, Goal>	get_actions_from_permutation_inner(const Goals& goals,
		const Agent_Id& acting_agent, const Paths& paths, const State& state);
	Paths									get_all_paths(const std::vector<Recipe>& recipes, const State& state);
	Collaboration_Info						get_best_collaboration(const std::vector<Collaboration_Info>& infos,
		const std::vector<Collaboration_Info>& probable_infos, const State& state);
	std::vector<Collaboration_Info>			get_collaboration_permutations(const Goals& goals,
		const Paths& paths, const std::vector<Agent_Combination>& agent_permutations,
		const State& state);
	Permutations							get_handoff_permutations() const;
	std::optional<std::vector<Action_Path>> get_permutation_action_paths(const Goals& goals,
		const Paths& paths) const;
	size_t									get_permutation_length(const Goals& goals, const Paths& paths);
	Action									get_random_good_action(const Collaboration_Info& info, const Paths& paths, const State& state);
	bool									ingredient_reachable(const Ingredient& ingredient_in, const Agent_Id agent,
		const Agent_Combination& agents, const State& state) const;
	bool									ingredients_reachable(const Recipe& recipe, const Agent_Id agent,
		const Agent_Combination& agents, const State& state) const;
	void									initialize_reachables(const State& initial_state);
	bool									is_agent_abused(const Goals& goals, const Paths& paths) const;
	bool									is_conflict_in_permutation(const State& initial_state,
		const std::vector<Joint_Action>& actions);
	bool									is_agent_subset_faster(const Collaboration_Info& info,
		const std::map<Goals, float>& goal_values);
	Paths									perform_new_search(const State& state, const Goal& goal,
		const Paths& paths, const std::vector<Joint_Action>& joint_actions, const Agent_Combination& acting_agents, const Action& initial_action = {});
	bool									temp(const Agent_Combination& agents, const Agent_Id& handoff_agent, const Recipe& recipe, const State& state);
	void									trim_trailing_non_actions(std::vector<Joint_Action>& joint_actions,
		const Agent_Id& handoff_agent);
	void									update_recogniser(const Paths& paths, const State& state);


	Recogniser recogniser;
	Search search;
	std::map<std::pair<Agent_Id, Agent_Combination>, Reachables> agent_reachables;
	size_t time_step;
};
