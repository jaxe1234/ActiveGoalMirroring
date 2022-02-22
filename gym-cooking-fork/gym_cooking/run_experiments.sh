#!/bin/bash

levels=("full-divider_salad" "partial-divider_salad" "open-divider_salad" "full-divider_tomato" "partial-divider_tomato" "open-divider_tomato" "full-divider_tl" "partial-divider_tl" "open-divider_tl" "full-divider_onionsalad" "partial-divider_onionsalad" "open-divider_onionsalad"  "full-divider_cucumbersalad" "partial-divider_cucumbersalad" "open-divider_ocucumbersalad")

models=("prap" "bd" "dc" "fb" "up" "greedy")

nagents=2
nseed=20
printf -v date_str '%(%Y-%m-%d-%H-%M-%S)T\n' -1

for seed in $(seq 1 1 $nseed); do
    for level in "${levels[@]}"; do
        for model1 in "${models[@]}"; do
            for model2 in "${models[@]}"; do
                echo python3 main.py --num-agents $nagents --seed $seed --level $level --model1 $model1 --model2 $model2
                python3 main.py --num-agents $nagents --seed $seed --level $level --model1 $model1 --model2 $model2 --result_file $date_str
                sleep 5
            done
        done
    done
done
