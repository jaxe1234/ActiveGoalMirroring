data_folder="results_bd_new"
plots_folder="$plots_folder"
if [ ! -z $1 ]
then
    data_folder="results_${1}_data"
    plots_folder="results_${1}_plots"
fi

python compile_graphs.py --ignore-order --in-folder $data_folder --out-folder $plots_folder
python compile_graphs.py --ignore-order --in-folder $data_folder --out-folder $plots_folder --individual
python compile_graphs.py --ignore-order --in-folder $data_folder --out-folder $plots_folder --time
python compile_graphs.py --ignore-order --in-folder $data_folder --out-folder $plots_folder --time --individual
python compile_graphs.py --ignore-order --in-folder $data_folder --out-folder $plots_folder --solved
python compile_graphs.py --ignore-order --in-folder $data_folder --out-folder $plots_folder --solved --individual
python compile_graphs.py --ignore-order --in-folder $data_folder --out-folder $plots_folder --solved-count
python compile_graphs.py --ignore-order --in-folder $data_folder --out-folder $plots_folder --solved-count --individual