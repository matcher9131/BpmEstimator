using System.Diagnostics;

namespace BpmEstimator;

static class Program
{
    /// <summary>
    /// 1行の文字列として入力されたコマンドライン引数を分けて配列にする
    /// </summary>
    /// <param name="line">入力文字列</param>
    /// <returns>引数の配列</returns>
    static IEnumerable<string> SplitArguments(string line)
    {
        line += " ";

        int left = 0;
        bool isInnerQuote = false;
        for (int i = 0; i < line.Length; ++i)
        {
            if (line[i] == ' ' && !isInnerQuote)
            {
                string arg = line[left..i];
                if (arg != string.Empty)
                {
                    yield return arg;
                }
                left = i + 1;
            }
            else if (line[i] == '"')
            {
                isInnerQuote = !isInnerQuote;
            }
        }
    }

    /// <summary>
    /// コマンドライン引数が空のとき、ユーザーに入力を尋ねる
    /// </summary>
    /// <returns>ユーザーの入力内容</returns>
    static IEnumerable<string> AskArguments()
    {
        Console.Write("Input: ");
        string line = Console.ReadLine()!;
        return SplitArguments(line);
    }

    /// <summary>
    /// コマンドライン引数をパースしてパラメータを得る
    /// </summary>
    /// <param name="args">コマンドライン引数</param>
    /// <returns>各パラメータのタプル</returns>
    static (string filePath, decimal minBpm, decimal maxBpm, decimal step) ParseArguments(IEnumerable<string> args)
    {
        const string ArgMinBpm = "-min=";
        const string ArgMaxBpm = "-max=";
        const string ArgStep = "-step=";

        string filePath = "";
        decimal minBpm = 60.0m;
        decimal maxBpm = 240.0m;
        decimal step = 1.0m;

        foreach (string arg in args)
        {
            if (arg.StartsWith(ArgMinBpm) && decimal.TryParse(arg.AsSpan(ArgMinBpm.Length), out decimal tempMinBpm) && tempMinBpm > 0)
            {
                minBpm = tempMinBpm;
            }
            else if (arg.StartsWith(ArgMaxBpm) && decimal.TryParse(arg.AsSpan(ArgMaxBpm.Length), out decimal tempMaxBpm) && tempMaxBpm > 0)
            {
                maxBpm = tempMaxBpm;
            }
            else if (arg.StartsWith(ArgStep) && decimal.TryParse(arg.AsSpan(ArgStep.Length), out decimal tempStep) && tempStep > 0)
            {
                step = tempStep;
            }
            else if (!arg.StartsWith("-"))
            {
                filePath = arg.Trim('"');
            }
        }

        return (filePath, minBpm, maxBpm, step);
    }

    /// <summary>
    /// BPM推定を行う
    /// </summary>
    /// <param name="filePath">対象となるファイルパス</param>
    /// <param name="minbpm">最低BPM</param>
    /// <param name="maxBpm">最高BPM</param>
    /// <param name="step">BPMの増分</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    static async Task Run(string filePath, decimal minbpm, decimal maxBpm, decimal step)
    {
        Wav wav = filePath.EndsWith(".mp3") ? (await Wav.FromMp3File(filePath))
            : filePath.EndsWith(".wav") ? Wav.FromWavFile(filePath)
            : throw new ArgumentException("Invalid extension of filepath.");

        // Local Function
        void analyze(ReadOnlySpan<short> input)
        {
            var bpmMatches = Core.GetBpmMatches(input, wav.SamplesPerSecond, minbpm, maxBpm, step);
            // 周波数成分のベスト3をBPM候補として表示する
            var estimatedBpms = bpmMatches.OrderByDescending(x => x.value).Take(3);
            foreach (var (bpm, (amplitude, phase)) in estimatedBpms)
            {
                Console.WriteLine($"  Bpm = {bpm:F2}, Amplitude = {amplitude:F0}, Phase = {phase / (2.0 * Math.PI) * (60.0 / bpm):F3}sec");
            }
        }
        // End Local Function

        Console.WriteLine("Channel 1:");
        analyze(wav.Channel1);

        if (wav.Channel2 != null)
        {
            Console.WriteLine("Channel 2:");
            analyze(wav.Channel2);
        }
    }

    static async Task Main(string[] args)
    {
#if DEBUG
#else
        try
        {
#endif
            (string filepath, decimal minbpm, decimal maxBpm, decimal step) = args.Length > 0 ? ParseArguments(args) : ParseArguments(AskArguments());
            Stopwatch sw = new();
            sw.Start();
            await Run(filepath, minbpm, maxBpm, step);
            sw.Stop();
            Console.WriteLine($"Elasped: {sw.ElapsedMilliseconds}ms");
#if DEBUG
#else
    }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }
#endif

        Console.Write("Press any key to finish.");
        Console.ReadKey();
    }
}
