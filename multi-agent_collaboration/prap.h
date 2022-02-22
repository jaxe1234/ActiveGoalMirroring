#pragma once
#include "Planner.hpp"

class prap : public Planner_Impl
{
public:
	prap(Environment environment, Agent_Id agent, const State& initial_state, size_t seed = 0);
	virtual Action get_next_action(const State& state, bool print_state) override;
private:
};
