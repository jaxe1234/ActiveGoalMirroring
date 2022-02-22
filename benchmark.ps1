$prapArgs = "PRAPhistory", "PRAPpropertyheuristic";
$prapHistorySizes = @(1,6);
$agents = @("prap");
$maps = Get-ChildItem "levels\BD" | Foreach-Object {$_.BaseName};
$noOfAgents = 2;
$benchmarksToRun = 0;
$seeds = @(0, 1, 2, 3, 4);
$cpBenchmarksToRun = $agents.length * $agents.length * $maps.length * $seeds.length; #cross-play
$spBenchmarksToRun = ($agents.Length - 1) * $maps.Length * $seeds.Length; #BD & MAC self-play
$spBenchmarksToRun += $maps.Length * $seeds.Length * $prapHistorySizes.Length * $prapUsePropertyHeuristic.Length; #PRAP self-play
$progressTemplate = "Suite {0} Progress: Running benchmark {1} of {2}. Total: {3} of {4}";
$existsTemplate = "{0};2;{1};{2};None;None;{3}";

$outputFile = (Get-Location).Path + "\" + "results\benchmark.txt";
#New-Item $outputFile -f;

$existing = get-content $outputFile;

$cpCurrentBenchmark = 0;

cd "gym-cooking-fork\gym_cooking\"

# cross-play benchmarks
$commandTemplate = "py .\main.py --num-agents 2 --level {0} --model1 {1} --model2 {2} --seed {3} --PRAPhistory 1";

for($i = 0; $i -lt $agents.length; $i++){
    for($j = 0; $j -lt $agents.length; $j++){
        for($k = 0; $k -lt $maps.Length; $k++){
            for ($p = 0; $p -lt $seeds.Length; $p++){
                $cpCurrentBenchmark++;
				$cmd = $commandTemplate -f $maps[$k], $agents[$i], $agents[$j], $seeds[$p]
				if($existing -imatch ($existsTemplate -f $maps[$k], $agents[$i], $agents[$j], $seeds[$p])){					
					"Skipping: " + $cmd;
				}else{
					"Running: " + $cmd;
					Invoke-Expression $cmd | Tee-Object $outputFile -append
				}
                $progressTemplate -f "Cross-play", $cpCurrentBenchmark, $cpBenchmarksToRun, $cpCurrentBenchmark, ($cpBenchmarksToRun + $spBenchmarksToRun)
            }
        }
    }
}


# self-play benchmarks
$spCurrentBenchmark = 0;
$spCommandTemplate = "py .\main.py --num-agents 3 --level {0} --model1 {1} --model2 {1} --model3 {1} --seed {2} --PRAPhistory {3}";
$existsTemplate2 = "{0};3;{1};{2};{3};None;{3}";


function Selfplay-Benchmark {
	param(
        [Parameter()]
		[string] $agent,
        [Parameter()]
        [int] $prapHistory,
        [Parameter()]
        [bool] $usePropertyHeuristic
        
	)

    foreach ($map in $maps){
        foreach ($seed in $seeds){
            $spCurrentBenchmark++;
            $cmd = $spCommandTemplate -f $map, $agent, $seed, $prapHistory
            if($existing -imatch ($existsTemplate -f $map, $agent, $agent, $agent, $seed)){					
				"Skipping: " + $cmd;
			}else{
				"Running: " + $cmd;
				Invoke-Expression $cmd | Tee-Object $outputFile -append
			}
            $progressTemplate -f "Self-play", $spCurrentBenchmark, $spBenchmarksToRun, ($cpCurrentBenchmark + $spCurrentBenchmark), ($cpBenchmarksToRun + $spBenchmarksToRun)
        }
    }

}

foreach ($size in $prapHistorySizes){
    Selfplay-Benchmark -agent "prap" -prapHistory $size -usePropertyHeuristic $false;
}

Selfplay-Benchmark -agent "mac" -prapHistory 1 -usePropertyHeuristic $false;
Selfplay-Benchmark -agent "bd" -prapHistory 1 -usePropertyHeuristic $false;



cd ..\..