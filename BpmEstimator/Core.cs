using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BpmEstimator
{
    /// <summary>
    /// メインの処理を担当するユーティリティクラス
    /// </summary>
    public static class Core
    {
        /// <summary>
        /// フレームにおける平均音量を求める
        /// </summary>
        /// <param name="input">波形データ</param>
        /// <returns>平均音量</returns>
        private static double GetFrameVolume(ReadOnlySpan<short> input)
        {
            long sum = 0;
            for (int i = 0; i < input.Length; i++)
            {
                sum += input[i] * input[i];
            }
            return Math.Sqrt((double)sum / input.Length);
        }

        /// <summary>
        /// 波形データからフレームごとの平均音量を求める
        /// </summary>
        /// <param name="input">波形データ</param>
        /// <param name="samplesPerFrame">1フレームごとのサンプル数</param>
        /// <returns>フレームごとの平均音量</returns>
        private static double[] GetFrameVolumes(ReadOnlySpan<short> input, int samplesPerFrame)
        {
            int length = input.Length / samplesPerFrame;
            double[] result = new double[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = GetFrameVolume(input[(i * samplesPerFrame)..((i + 1) * samplesPerFrame)]);
            }
            return result;
        }

        /// <summary>
        /// フレームごとの平均音量の増分を取得する
        /// </summary>
        /// <param name="frameVolumes">フレームごとの平均音量</param>
        /// <returns>フレームごとの平均音量の増分</returns>
        private static double[] GetFrameVolumeIncrement(ReadOnlySpan<double> frameVolumes)
        {
            double[] result = new double[frameVolumes.Length];
            for (int i = 1; i < frameVolumes.Length; i++)
            {
                // 増加したときのみ増分を取る（減少したときは0）
                result[i] = frameVolumes[i] > frameVolumes[i - 1] ? frameVolumes[i] - frameVolumes[i - 1] : 0;
            }
            return result;
        }

        /// <summary>
        /// フレームごとの平均音量の増分の時間変化の周波数成分を求める
        /// </summary>
        /// <param name="frameVolumeIncrements">フレームごとの平均音量の増分</param>
        /// <param name="samplesPerFrame">1フレームごとのサンプル数</param>
        /// <param name="frequency">対象とする周波数</param>
        /// <param name="sampleFrequency">サンプリング周波数</param>
        /// <returns>振幅と位相のタプル</returns>
        private static (double amplitude, double phase) GetFrequencyMatch(ReadOnlySpan<double> frameVolumeIncrements, int samplesPerFrame, double frequency, double sampleFrequency)
        {
            double sumCos = 0, sumSin = 0;
            for (int i = 0; i < frameVolumeIncrements.Length; i++)
            {
                sumCos += frameVolumeIncrements[i] * Math.Cos(2.0 * Math.PI * frequency * i / (sampleFrequency / samplesPerFrame));
                sumSin += frameVolumeIncrements[i] * Math.Sin(2.0 * Math.PI * frequency * i / (sampleFrequency / samplesPerFrame));
            }
            double a = sumCos / samplesPerFrame;
            double b = sumSin / samplesPerFrame;
            return (Math.Sqrt(a * a + b * b), Math.Atan2(b, a));
        }

        /// <summary>
        /// 各BPMにおける周波数成分を求める
        /// </summary>
        /// <param name="input">波形データ</param>
        /// <param name="sampleFrequency">サンプリング周波数</param>
        /// <param name="minBpm">最低BPM</param>
        /// <param name="maxBpm">最高BPM</param>
        /// <param name="step">BPMの増分</param>
        /// <returns>(BPMの値, (振幅, 位相))のタプルの集合</returns>
        public static IEnumerable<(double bpm, (double amplitude, double phase) value)> GetBpmMatches(ReadOnlySpan<short> input, double sampleFrequency, decimal minBpm, decimal maxBpm, decimal step)
        {
            decimal roundedMinBpm = Math.Round(minBpm * 20.0m) / 20.0m;
            decimal roundedMaxBpm = Math.Round(maxBpm * 20.0m) / 20.0m;
            decimal roundedStep = Math.Round(step * 20.0m) / 20.0m;
            if (roundedStep == 0)
            {
                roundedStep = 0.05m;
            }
            int samplesPerFrame = roundedStep >= 0.5m ? 512
                : roundedStep >= 0.1m ? 128
                : 64;
            double[] frameVolumeIncrements = GetFrameVolumeIncrement(GetFrameVolumes(input, samplesPerFrame));
            return Enumerable.Range(0, (int)Math.Floor((roundedMaxBpm - roundedMinBpm) / roundedStep)).Select(i => roundedMinBpm + i * roundedStep).AsParallel().Select(bpm => ((double)bpm, GetFrequencyMatch(frameVolumeIncrements, samplesPerFrame, (double)bpm / 60.0, sampleFrequency)));
        }
    }
}
