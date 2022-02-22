#include "prap.h"

#include <utility>

prap::prap(Environment environment, Agent_Id agent, const State& initial_state, size_t seed)
	: Planner_Impl(std::move(environment), agent)
{
	
}

Action prap::get_next_action(const State& state, bool print_state)
{
	return {};
}
