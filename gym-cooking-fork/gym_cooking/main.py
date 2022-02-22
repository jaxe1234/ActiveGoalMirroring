import os
os.environ['PYGAME_HIDE_SUPPORT_PROMPT'] = "hide"

import envs.overcooked_environment
from recipe_planner.recipe import *
from utils.world import World
from utils.agent import RealAgent, SimAgent, COLORS
from utils.core import *
from misc.game.gameplay import GamePlay
from misc.metrics.metrics_bag import Bag
from timeit import default_timer as timer

import numpy as np
import random
import argparse
from collections import namedtuple

import gym

from mac_interface import mac_get_next_action, mac_init, mac_update, mac_add_agent
from datetime import datetime
import time

import sys
import os

from clr_loader import get_coreclr
from pythonnet import set_runtime

dll_path = os.path.join(os.getcwd(), f"..\\..\\MACsharp\\bin\\Release\\net5.0")
rt_path = os.path.join(os.getcwd(), f"..\\..\\MACsharp\\bin\\Release\\net5.0\\MACsharp.runtimeconfig.json")

set_runtime(get_coreclr(rt_path))
import clr
sys.path.append(dll_path)
clr.AddReference("MACsharp")
from MACsharp import MACsharpInterfacer


def parse_arguments():
    parser = argparse.ArgumentParser("Overcooked 2 argument parser")

    # Environment
    parser.add_argument("--level", type=str, required=True)
    parser.add_argument("--num-agents", type=int, required=True)
    parser.add_argument("--max-num-timesteps", type=int, default=100, help="Max number of timesteps to run")
    parser.add_argument("--max-num-subtasks", type=int, default=14, help="Max number of subtasks for recipe")
    parser.add_argument("--seed", type=int, default=1, help="Fix pseudorandom seed")
    parser.add_argument("--with-image-obs", action="store_true", default=False, help="Return observations as images (instead of objects)")

    # Delegation Planner
    parser.add_argument("--beta", type=float, default=1.3, help="Beta for softmax in Bayesian delegation updates")

    # Navigation Planner
    parser.add_argument("--alpha", type=float, default=0.01, help="Alpha for BRTDP")
    parser.add_argument("--tau", type=int, default=2, help="Normalize v diff")
    parser.add_argument("--cap", type=int, default=75, help="Max number of steps in each main loop of BRTDP")
    parser.add_argument("--main-cap", type=int, default=100, help="Max number of main loops in each run of BRTDP")

    # Visualizations
    parser.add_argument("--play", action="store_true", default=False, help="Play interactive game with keys")
    parser.add_argument("--record", action="store_true", default=False, help="Save observation at each time step as an image in misc/game/record")

    # PRAP
    parser.add_argument("--PRAPhistory", type=int, default=6, help="The PRAP historical data kept for inertia")
    parser.add_argument("--PRAPpropertyheuristic", type=bool, default=False, help="Use the property count heuristic")
    parser.add_argument("--PRAPprint", type=bool, default=False, help="Print PRAP debugging or information")
    parser.add_argument("--PRAPerror", type=bool, default=False, help="Print PRAP error messages")

    # Models
    # Valid options: `bd` = Bayes Delegation; `up` = Uniform Priors
    # `dc` = Divide & Conquer; `fb` = Fixed Beliefs; `greedy` = Greedy
    parser.add_argument("--model1", type=str, default=None, help="Model type for agent 1 (bd, up, dc, fb, or greedy)")
    parser.add_argument("--model2", type=str, default=None, help="Model type for agent 2 (bd, up, dc, fb, or greedy)")
    parser.add_argument("--model3", type=str, default=None, help="Model type for agent 3 (bd, up, dc, fb, or greedy)")
    parser.add_argument("--model4", type=str, default=None, help="Model type for agent 4 (bd, up, dc, fb, or greedy)")

    parser.add_argument("--result_file", type=str, default=None, help="Which file to save the one line summary of the run in")

    return parser.parse_args()


def fix_seed(seed):
    np.random.seed(seed)
    random.seed(seed)

def initialize_agents(arglist):
    real_agents = []

    with open('utils/levels/{}.txt'.format(arglist.level), 'r') as f:
        phase = 1
        recipes = []
        for line in f:
            line = line.strip('\n')
            if line == '':
                phase += 1

            # phase 2: read in recipe list
            elif phase == 2:
                recipes.append(globals()[line]())

            # phase 3: read in agent locations (up to num_agents)
            elif phase == 3:
                if len(real_agents) < arglist.num_agents:
                    loc = line.split(' ')
                    real_agent = RealAgent(arglist=arglist,
                            name='agent-' + str(len(real_agents) + 1),
                            id_color=COLORS[len(real_agents)],
                            recipes=recipes)
                    real_agents.append(real_agent)

    return real_agents

def main_loop(arglist, folder, macsharpInterfacer):

    """The main loop for running experiments."""
    #print("Initializing environment and agents.")
    env = gym.envs.make("gym_cooking:overcookedEnv-v0", arglist=arglist)
    obs = env.reset()
    # game = GameVisualize(env)
    real_agents = initialize_agents(arglist=arglist)

    # Info bag for saving pkl files
    bag = Bag(arglist=arglist, filename=env.filename)
    bag.set_recipe(recipe_subtasks=env.all_subtasks)

    today = datetime.now()
    today_str = today.strftime('%Y-%m-%d-%H-%M-%S')
    #print(F"Folder is: {folder}")
    if not os.path.exists(F"results/{folder}"):
        os.makedirs(F"results/{folder}")
    output_file = F"results/{folder}/{env.filename}.txt"
    with open(output_file, "w") as file:
        file.write(F"{arglist.level}\n")
        for agent_type in list(filter(lambda x: x is not None, model_types)):
            file.write(F"{agent_type}\n")

    action_count = 0
    actions_list = []
    error = "None"    
    time_start = timer()

    if 'onion' in arglist.level and 'mac' in model_types:
        time_end = timer()
        return [action_count, False, error, time_end - time_start]

    try:
        while not env.done():
            # Timeout of 3 hours
            if timer() - time_start > 10800:
                error = "Out of time"
                action_count += 100
            if action_count >= 100:
                break
            action_count += 1
            action_dict = {}
            action_list = []
            
            for index, agent in enumerate(real_agents):
                if agent.model_type == 'mac':
                    action = mac_get_next_action({ 'agent_id' : index })
                elif agent.model_type == 'prap':
                    tempaction = macsharpInterfacer.GetNextAction(index)
                    action = (tempaction.Item1, tempaction.Item2)
                else:
                    action = agent.select_action(obs=obs)
                action_dict[agent.name] = action
                action_list.append(action)

            temp = mac_update({'actions':action_list})
            actions_list.append(action_list)

            obs, reward, done, info = env.step(action_dict=action_dict)

            for index, action_key in enumerate(obs.agent_actions):
                (x, y) = obs.agent_actions[action_key]
                macsharpInterfacer.Update(index, x, y)

            # Agents
            for index, agent in enumerate(real_agents):
                if agent.model_type not in ['mac', 'still', 'prap']:
                    agent.refresh_subtasks(world=env.world)
    except Exception as e:
        #print(F"Python error: {e}")
        action_count += 100
        error = str(e)

    #print(actions_list)
    
    time_end = timer()

    with open(output_file, "a") as file:
        file.write(F"{action_count}\n")
    return [action_count, env.successful, error, time_end - time_start]
        # Saving info
        #bag.add_status(cur_time=info['t'], real_agents=real_agents)


    # Saving final information before saving pkl file
    #bag.set_collisions(collisions=env.collisions)
    #bag.set_termination(termination_info=env.termination_info,
    #        successful=env.successful)
def write_result_summary(output_file, level, model1, model2, seed, action_count, time):
    while(True):
        try: 
            with open(output_file, "a") as file:
                file.write(F"{level};{model1};{model2};{seed};{action_count};{time}\n")
                break
        except IOError:
            time.sleep(3)


if __name__ == '__main__':
    arglist = parse_arguments()
    if arglist.play:
        env = gym.envs.make("gym_cooking:overcookedEnv-v0", arglist=arglist)
        env.reset()
        game = GamePlay(env.filename, env.world, env.sim_agents)
        game.on_execute()
    else:   
        model_types = [arglist.model1, arglist.model2, arglist.model3, arglist.model4]

        macsharpInterfacer = MACsharpInterfacer()
        macsharpInterfacer.SetSeed(arglist.seed)
        macsharpInterfacer.LoadLevel(arglist.level, arglist.num_agents)

        # load PRAP args
        macsharpInterfacer.SetPrapHistorySize(arglist.PRAPhistory)
        macsharpInterfacer.SetUsePropertyCountHeuristic(arglist.PRAPpropertyheuristic)
        macsharpInterfacer.SetPrint(arglist.PRAPprint)
        macsharpInterfacer.SetPrintError(arglist.PRAPerror)        

        if 'mac' in model_types and 'onion' not in arglist.level:
            temp = mac_init({'file_name':arglist.level, 'agent_size':arglist.num_agents, 'seed':arglist.seed})
        
        for index, model_type in enumerate(model_types):
            if model_type == 'mac':
                temp = mac_add_agent({'agent_id':index})             
            if model_type == 'prap':
                macsharpInterfacer.InitializeAgent(index, model_type)

        assert len(list(filter(lambda x: x is not None,
            model_types))) == arglist.num_agents, "num_agents should match the number of models specified"
        fix_seed(seed=arglist.seed)
        today = datetime.now()
        today_str = today.strftime('%Y-%m-%d-%H-%M-%S')
        folder = F"{today_str}"

        if arglist.result_file is not None:
            folder = arglist.result_file
        else:
            folder = today_str

        [action_count, solved, error, used_time] = main_loop(arglist=arglist, folder=folder, macsharpInterfacer=macsharpInterfacer)
        
        print(F"{arglist.level};{arglist.num_agents};{arglist.model1};{arglist.model2};{arglist.model3};{arglist.model4};{arglist.seed};{action_count};{used_time};{solved};{arglist.PRAPhistory};{arglist.PRAPpropertyheuristic};{error};")
        if arglist.result_file is not None:
            output_file = F"results_summary/{arglist.result_file}.txt"
            write_result_summary(output_file, arglist.level, arglist.model1, arglist.model2, arglist.seed, action_count, F"{used_time}")



