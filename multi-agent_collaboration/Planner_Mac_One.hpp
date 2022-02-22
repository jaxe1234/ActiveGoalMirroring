#pragma once

#include "Environment.hpp"
#include "Core.hpp"
#include "Search.hpp"
#include "State.hpp"
#include "Recogniser.hpp"
#include "Planner.hpp"
#include "Planner_Mac.hpp"

#include <vector>
#include <set>

class Planner_Mac_One : public Planner_Impl {


public:
	Planner_Mac_One(Environment environment, Agent_Id agent, const State& initial_state, size_t seed = 0);
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