data_folder="results_bd_new"
plots_folder="$plots_folder"
if [ ! -z $1 ]
then
    data_folder="results_${1}_data"
    plots_folder="results_${1}_plots"
fi

python compile_graphs.py --ignore-order --in-folder $data_folder --out-folder $plots_folder --x 
python compile_graphs.py --ignore-order --in-folder $data_folder --out-folder $plots_folder --x --individual
python compile_graphs.py --ignore-order --in-folder $data_folder --out-folder $plots_folder --x --time
python compile_graphs.py --ignore-order --in-folder $data_folder --out-folder $plots_folder --x --time --individual
python compile_graphs.py --ignore-order --in-folder $data_folder --out-folder $plots_folder --x --solved
python compile_graphs.py --ignore-order --in-folder $data_folder --out-folder $plots_folder --x --solved --individual
python compile_graphs.py --ignore-order --in-folder $data_folder --out-folder $plots_folder --x --solved-count
python compile_graphs.py --ignore-order --in-folder $data_folder --out-folder $plots_folder --x --solved-count --individual