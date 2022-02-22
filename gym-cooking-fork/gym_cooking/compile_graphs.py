import sys
import glob
import os
import seaborn as sns
import matplotlib.pyplot as plt
import pandas as pd
from statistics import stdev
from scipy.stats import sem
import argparse


def parse_arguments():
    parser = argparse.ArgumentParser("Graph compilation parser")
    parser.add_argument("--in-folder", dest="in_folder", type=str, default="results_bd", help="The folder to look for data")
    parser.add_argument("--out-folder", dest="out_folder", type=str, default="results_plots", help="The folder to output plots")
    parser.add_argument("--ignore-order", dest="ignore_order", action="store_true", default=False, help="If true models (i,j)==(j,i)")
    feature_parser = parser.add_mutually_exclusive_group(required=False)
    feature_parser.add_argument("--file", type=str, default=None, help="If only a single file should be compiled")
    feature_parser.add_argument('--latest', action='store_true', help="If only the latest file should be compiled")
    parser.add_argument("--individual", action="store_true", default=False, help="If true a heatmap is produced for every level")
    feature_parser2 = parser.add_mutually_exclusive_group(required=False)
    feature_parser2.add_argument("--solved", action="store_true", default=False, help="If true only solved instances are used")
    feature_parser2.add_argument("--solved-count", dest="solved_count", action="store_true", default=False, help="Print amount of solved levels")
    feature_parser2.add_argument("--time", action="store_true", default=False, help="Print time taken")
    parser.add_argument("--x", action="store_true", default=False, help="If true models will use macx and mac1")
    return parser.parse_args()


def handle_data(folder, file_name, data, arglist):
    avg_data = []
    labels = [['' for entry2 in entry1] for entry1 in data]
    for index1, entry1 in enumerate(data):
        avg_data.append([])
        for index2, entry2 in enumerate(entry1):
            # print(entry2)
            data_point = sum(entry2)/len(entry2) if len(entry2) > 0 else 0
            # print(F"{models[index1]}:{models[index2]}={data_point}")


            if arglist.solved_count:

                levels_count = 1 if arglist.individual else arglist.total_levels
                total_runs = levels_count * (arglist.max_seed - arglist.min_seed + 1)
                if index1 != index2:
                    total_runs *= 2
                # print(F"index1:{index1}, index2: {index2}")

                if len(entry2) > 0:
                    solved = 100 * sum([1 if temp < 100 else 0 for temp in entry2]) / len(entry2)
                    # solved = 100 * sum([1 if temp < 100 else 0 for temp in entry2]) / total_runs
                else:
                    solved = 0
                data_point = solved
                label_point = '' if index1 < index2 else "{:.1f}".format(solved)
                # label_point += "%"
                # solved = sum ([1 if temp < 100 else 0 for temp in entry2])
                # if models[index1] != 'mac' and models[index2] != 'mac':
                #     solved = solved / 20
                # if index1 == index2:
                #     solved = solved * 2

                # label_point = F"{solved}"
            elif len(entry2) > 1:
                #label_point = F"{round(sum(entry2)/len(entry2))}\n+/-{round(stdev(entry2) / math.sqrt(len(entry2)),1)}"
                if arglist.time:
                    # label1 = "{:.1E}".format(round(sum(entry2)/len(entry2)))
                    # label2 = "{:.1E}".format(round(sem(entry2),1))
                    # label_point = F"{label1}\n+/-{label2}"
                    data_point = round((sum(entry2)/len(entry2)) / 1000)
                    label_point = F"{data_point}"
                    # label_point += F"\n+/-{round(sem(entry2) / 1000)}"
                else:
                    data_point = round(sum(entry2)/len(entry2), 1)
                    label_point = F"{data_point}"
                    # label_point += F"\n+/-{round(sem(entry2),1)}"
            elif len(entry2) == 1:
                if arglist.time:
                    # label1 = "{:.1E}".format(entry2[0])
                    # label_point = F"{label1}\n+/-0"
                    data_point = entry2[0] / 1000
                    label_point = F"{data_point}"
                    # label_point += F"\n+/-0"
                else:
                    data_point = entry2[0]
                    label_point = F"{data_point}"
                    # label_point += F"\n+/-0"
            else:
                # data_point = 0 if arglist.solved_count else 100
                # label_point = '' if index1 < index2 else F"{data_point}\n+/-0"
                data_point = 0
                label_point = '' if index1 < index2 else F"n/a"

            # labels[-1].append(label_point)
            # labels[index2][index1] = label_point
            labels[index2][index1] = label_point
            avg_data[-1].append(data_point)


    # avg_data = [[sum(entry2)/len(entry2) if len(entry2) > 0 else 0 for entry2 in entry1] for entry1 in data]
    # labels = [[F"{round(sum(entry2)/len(entry2))}\n+/-{round(stdev(entry2),1)}" if len(entry2) > 0 else '0\nn/a' for entry2 in reversed(entry1)] for entry1 in data]
    # labels = [[F"{round(sum(entry2)/len(entry2))}\n+/-{round(stdev(entry2),1)}" if len(entry2) > 0 else '0\nn/a' for entry2 in entry1] for entry1 in data]

    temp = {}
    for index, row in list(enumerate(avg_data)):
        temp[models[index]] = row
    df = pd.DataFrame(temp, models)

    # mask = [[False if index1 < index2 else True for index1 in range(len(models))] for index2 in range(len(models))]
    mask = {model2 : [True if index1 > index2 else False for index1,_ in enumerate(models)] for index2,model2 in enumerate(models)}
    # mask[models[-1]][-1] = False
    mask_df = pd.DataFrame(mask, models)

    #ax = sns.heatmap(df, annot=True)
    if arglist.time:
        ax = sns.heatmap(df, mask=mask_df, annot=labels, fmt='', cmap='Blues', cbar_kws={"pad":0.01}, annot_kws={"fontsize":11}, vmin=0)
    else:
        ax = sns.heatmap(df, mask=mask_df, annot=labels, fmt='', cmap='Blues', cbar_kws={"pad":0.01}, annot_kws={"fontsize":12}, vmin=20, vmax=100)
    if not os.path.exists(folder):
        os.makedirs(folder)

    if arglist.solved:
        file_name_extension = "_solved"
    elif arglist.solved_count:
        file_name_extension = "_solved_count"
    elif arglist.time:
        file_name_extension = "_time"
    else:
        file_name_extension = ''

    file = folder + "\\" + file_name + file_name_extension + ".png"
    plt.savefig(file, bbox_inches = 'tight',pad_inches = 0)
    plt.close()


if __name__=='__main__':
    arglist = parse_arguments()
    folder = arglist.in_folder

    # Get files
    files = []
    if arglist.file is not None:
        files = [F"{folder}/{arglist.file}.txt"]
    elif arglist.latest:
        list_of_files = glob.glob(F"{folder}/*.txt")
        files = [max(list_of_files, key=os.path.getmtime)]
    else:
        files = glob.glob(F"{folder}/*.txt")

    # Note model indices
    # models=["mac", "still", "bd", "dc", "fb", "up", "greedy"]
    # models=["mac", "bd", "up", "fb", "dc", "greedy", "still"]
    if arglist.x:
        models=["mac", "mac1", "bd", "fb", "up", "dc", "greedy", "still"]
    else:
        models=["mac", "bd", "fb", "up", "dc", "greedy", "still"]

    indices = {}
    for index, model in enumerate(models):
        indices[model] = index

    # Get data from files
    min_seed = None
    max_seed = None
    levels = []
    models_found = []

    data = {}
    for file_name in files:
        with open(file_name, 'r') as file:
            lines = file.readlines()
            for index, line in enumerate(lines):

                entries = line.split(";")
                if len(entries) < 5:
                    print(F"Bad format data {entries}\n")
                    continue

                level = entries[0]
                seed = int(entries[3])
                if max_seed is None or seed > max_seed:
                    max_seed = seed
                if min_seed is None or seed < min_seed:
                    min_seed = seed
                if level not in levels:
                    levels.append(level)

                if level not in data:
                    data[level] = []
                    for index1, _ in enumerate(models):
                        data[level].append([])
                        for index2, _ in enumerate(models):
                            data[level][index1].append([])

                index1 = indices[entries[1]]
                index2 = indices[entries[2]]
                if arglist.ignore_order:
                    if index1 < index2:
                        temp = index1
                        index1 = index2
                        index2 = temp
                if int(entries[4]) >= 100:
                    # if int(entries[4]) > 100:
                    #     print(line)
                    if arglist.solved:
                        continue
                    else:
                        entries[4] = 100
                if arglist.time:
                    if len(entries) > 5:
                        data[level][index1][index2].append(int(entries[5]))
                else:
                    data[level][index1][index2].append(int(entries[4]))

    arglist.min_seed = min_seed
    arglist.max_seed = max_seed
    arglist.total_levels = len(levels)
    # Parse data
    if arglist.individual:
        for level, data_entry in data.items():
            handle_data(arglist.out_folder, level, data_entry, arglist)
    else:
        new_data = []
        for index1, _ in enumerate(models):
            new_data.append([])
            for index2, _ in enumerate(models):
                new_data[index1].append([])

        for level, data_entry in data.items():              # Levels
            for index1, entry1 in enumerate(data_entry):    # Model1
                for index2, entry2 in enumerate(entry1):    # Model2
                    for value in entry2:                    # Seed
                        new_data[index1][index2].append(value)

        handle_data(arglist.out_folder, 'total', new_data, arglist)

folder = 'results_summary'
if len(sys.argv) > 1:
    file_name = F"{folder}/{sys.argv[1]}"
else:
    list_of_files = glob.glob(F"{folder}/*.txt")
    file_name = max(list_of_files, key=os.path.getctime)

models=["mac", "still", "bd", "dc", "fb", "up", "greedy"]
indices = {}
for index, model in enumerate(models):
    indices[model] = index

data = []
for index1, _ in enumerate(models):
    data.append([])
    for index2, _ in enumerate(models):
        data[index1].append([])

with open(file_name, 'r') as file:
    lines = file.readlines()
    for index, line in enumerate(lines):
        if index == 0:
            headers = line[0]
            continue
        entries = line.split(";")
        index1 = indices[entries[1]]
        index2 = indices[entries[2]]
        data[index1][index2].append(int(entries[4]))

        #print(F"{index} {line}")


avg_data = [[sum(entry2)/len(entry2) if len(entry2) > 0 else 0 for entry2 in entry1] for entry1 in data]

temp = {}
for index, row in reversed(list(enumerate(avg_data))):
    temp[models[index]] = row
df = pd.DataFrame(temp, models)

ax = sns.heatmap(df, annot=True)
folder = 'results_plots'
if not os.path.exists(folder):
    os.makedirs(folder)
file_name = file_name.split("\\")[-1]
file = folder + "\\" + file_name[:-4] + ".png"
plt.savefig(file)
temp = 0

