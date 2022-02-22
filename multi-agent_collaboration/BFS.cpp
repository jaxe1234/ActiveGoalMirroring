#include "BFS.hpp"
#include "Search.hpp"

#include <deque>
#include <unordered_set>
#include <stdexcept>

std::vector<Joint_Action> BFS::search_joint(const State& state,
	Recipe recipe, const Agent_Combination& agents, Agent_Id handoff_agent,
	const std::vector<Joint_Action>& input_actions, const Agent_Combination& free_agents, const Action& initial_action) {

	if (handoff_agent.is_not_empty()) {
		throw std::runtime_error("Handoff agent not supported for bfs");
	}

	if (initial_action.has_value()) {
		throw std::runtime_error("Initial action not supported for bfs");
	}

	Search_Joint_State dummy(state, { }, 0, 0);
	std::unordered_set<Search_Joint_State> visited;
	std::vector<Search_Joint_State> path;
	std::deque<Search_Joint_State> frontier;
	path.push_back(dummy);
	visited.insert(dummy);
	frontier.push_back(dummy);
	bool done = false;
	size_t state_id = 1;
	size_t goal_id = 0;

	auto actions = environment.get_joint_actions(agents);
	while (!done) {
		// No possible path
		if (frontier.empty()) {
			return {};
		}
		auto current_state = frontier.front();
		frontier.pop_front();

		for (const auto& action : actions) {
			auto temp_state = current_state;
			environment.act(temp_state.state, action, Print_Level::NOPE);
			auto search_state = Search_Joint_State(temp_state.state, action, current_state.id, state_id);
			if (visited.find(search_state) == visited.end()) {
				visited.insert(search_state);
				path.push_back(search_state);
				frontier.push_back(search_state);
				if (temp_state.state.contains_item(recipe.result)) {
					done = true;
					goal_id = state_id;
					break;
				}
				++state_id;
			}
		}
	}
	return extract_actions<Search_Joint_State>(goal_id, path);
}

std::pair<size_t, Direction> BFS::get_dist_direction(Coordinate source, Coordinate dest, size_t walls) {
	throw std::runtime_error("Not implemented");
}
