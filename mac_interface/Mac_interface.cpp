#include <Python.h>
#include <iostream>
#include <string>
#include <vector>
#include <map>
#include <iterator>
#include <exception>

#include "Environment.hpp"
#include "State.hpp"
#include "Planner_Mac.hpp"
#include "Planner.hpp"
#include "Core.hpp"

size_t time_step;
Environment environment(2);
std::vector<Planner> planners;
std::map<size_t, int> planner_map;
State state;
size_t seed;
int mac_agents;

//std::vector<Direction> fixed_actions{ Direction::DOWN, Direction::LEFT, Direction::UP, Direction::RIGHT };

std::vector<size_t> list_to_long_vector(PyObject* incoming) {
	//assert(PyList_Check(incoming));

	std::vector<size_t> data;
	if (PyList_Check(incoming)) {
		for (Py_ssize_t i = 0; i < PyList_Size(incoming); i++) {
			PyObject* value = PyList_GetItem(incoming, i);
			data.push_back(PyLong_AsLong(value));
		}
	}
	else if (PyTuple_Check(incoming)) {
		for (Py_ssize_t i = 0; i < PyTuple_Size(incoming); i++) {
			PyObject* value = PyTuple_GetItem(incoming, i);
			data.push_back(PyLong_AsLong(value));
		}
	}
	else {
		throw std::runtime_error("Unknown datatype");
	}
	return data;
}

std::vector<PyObject*> list_to_action_vector(PyObject* incoming) {
	//assert(PyList_Check(incoming));
	std::vector<PyObject*> data;
	if (PyList_Check(incoming)) {
		for (Py_ssize_t i = 0; i < PyList_Size(incoming); i++) {
			data.push_back(PyList_GetItem(incoming, i));
		}
	}
	else if (PyTuple_Check(incoming)) {
		std::vector<PyObject*> data;
		for (Py_ssize_t i = 0; i < PyTuple_Size(incoming); i++) {
			data.push_back(PyTuple_GetItem(incoming, i));
		}
	}
	else {
		throw std::runtime_error("Unknown datatype");
	}
	return data;
}

std::string to_string(PyObject* obj) {
	PyObject* repr = PyObject_Repr(obj);
	PyObject* str = PyUnicode_AsEncodedString(repr, "utf-8", "~E~");
	const char* bytes = PyBytes_AsString(str);
	std::string result(bytes);
	Py_XDECREF(repr);
	Py_XDECREF(str);
	return result;
}

PyObject* mac_init(PyObject*, PyObject* o) {
	set_logging_enabled();
	time_step = 0;
	mac_agents = 0;

	auto file_name = to_string(PyDict_GetItemString(o, "file_name"));
	file_name = std::string(file_name.begin() + 1, file_name.end() - 1);
	file_name = "utils/levels/" + file_name + ".txt";

	size_t agent_size = PyLong_AsLong(PyDict_GetItemString(o, "agent_size"));

	environment = Environment(agent_size);
	state = environment.load(file_name);

	seed = PyLong_AsLong(PyDict_GetItemString(o, "seed"));

	return PyLong_FromLong(2);
}

PyObject* mac_add_agent(PyObject*, PyObject* o) {
	size_t agent_id = (size_t)PyLong_AsLong(PyDict_GetItemString(o, "agent_id"));
	planners.emplace_back(std::make_unique<Planner_Mac>(environment, agent_id, state, seed));
	mac_agents++;
	planner_map.emplace(agent_id, mac_agents);

	return PyLong_FromLong(2);
}

PyObject* action_mac_to_bd(Action action) {
	PyObject* list = PyTuple_New(2);
	PyTuple_SetItem(list, 0, PyLong_FromLong(0));
	PyTuple_SetItem(list, 1, PyLong_FromLong(0));
	std::vector<int> direction;
	switch (action.direction) {
	case Direction::UP:		PyTuple_SetItem(list, 1, PyLong_FromLong(-1)); break;
	case Direction::RIGHT:	PyTuple_SetItem(list, 0, PyLong_FromLong(1)); break;
	case Direction::DOWN:	PyTuple_SetItem(list, 1, PyLong_FromLong(1)); break;
	case Direction::LEFT:	PyTuple_SetItem(list, 0, PyLong_FromLong(-1)); break;
	}
	return list;

}

Action action_bd_to_mac(PyObject* o, Agent_Id input_agent) {
	auto vec = list_to_long_vector(o);
	assert(vec.size() == 2);
	switch (vec.at(0)) {
	case 1: return { Direction::RIGHT, input_agent };
	case -1: return { Direction::LEFT, input_agent };
	}
	switch (vec.at(1)) {
	case 1: return { Direction::DOWN, input_agent };
	case -1: return { Direction::UP, input_agent };
	}
	return { Direction::NONE, input_agent };
}

PyObject* mac_update(PyObject*, PyObject* o) {

	if (planners.empty()) {
		return PyLong_FromLong(2);
	}

	++time_step;
	auto actions_raw = list_to_action_vector(PyDict_GetItemString(o, "actions"));
	std::vector<Action> actions;
	for (size_t i = 0; i < actions_raw.size(); ++i) {
		actions.push_back(action_bd_to_mac(actions_raw.at(i), Agent_Id{ i }));
	}
	environment.act(state, { actions });
	return PyLong_FromLong(2);
}

PyObject* mac_get_next_action(PyObject*, PyObject* o) {
	size_t agent_id = (size_t)PyLong_AsLong(PyDict_GetItemString(o, "agent_id"));

	try {
		auto planner_index = planner_map.at(agent_id) - 1;
		auto action = planners.at(planner_index).get_next_action(state, true);
		return action_mac_to_bd(action);
	}
	catch (std::exception& e)
	{
		throw std::runtime_error(e.what());
	}

	return PyLong_FromLong(2);	
}

static PyMethodDef mac_interface_methods[] = {
	// The first property is the name exposed to Python, fast_tanh, the second is the C++
	// function name that contains the implementation.
	{ "mac_init", (PyCFunction)mac_init, METH_O, nullptr },
	{ "mac_add_agent", (PyCFunction)mac_add_agent, METH_O, nullptr },
	{ "mac_update", (PyCFunction)mac_update, METH_O, nullptr },
	{ "mac_get_next_action", (PyCFunction)mac_get_next_action, METH_O, nullptr },

	// Terminate the array with an object containing nulls.
	{ nullptr, nullptr, 0, nullptr }
};

static PyModuleDef mac_interface_module = {
	PyModuleDef_HEAD_INIT,
	"mac_interface",                        // Module name to use with Python import statements
	"Interface for the thesis agent",       // Module description
	0,
	mac_interface_methods                   // Structure that defines the methods of the module
};

PyMODINIT_FUNC PyInit_mac_interface() {
	return PyModule_Create(&mac_interface_module);
}