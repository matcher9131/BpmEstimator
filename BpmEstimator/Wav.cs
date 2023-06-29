using System.Runtime.InteropServices;

namespace BpmEstimator;

/// <summary>
/// WAVファイルデータ
/// </summary>
public class Wav
{
    /// <summary>
    /// 指定したバイト列から<see cref="Wav"/>クラスの新しいインスタンスを作成する
    /// </summary>
    /// <param name="buffer">バイト列</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public Wav(Span<byte> buffer)
    {
        if (MemoryMarshal.Read<int>(buffer[0..4]) != 0x46464952) throw new ArgumentException("Buffer is not a wav format.");    // 'RIFF'
        if (MemoryMarshal.Read<int>(buffer[8..12]) != 0x45564157) throw new ArgumentException("Buffer is not a wav format.");   // 'WAVE'
        int position = 12;
        while (position < buffer.Length)
        {
            int chunkHeader = MemoryMarshal.Read<int>(buffer[position..(position + 4)]);
            int chunkSize = MemoryMarshal.Read<int>(buffer[(position + 4)..(position + 8)]);
            Span<byte> chunk = buffer[(position + 8)..(position + 8 + chunkSize)];
            switch (chunkHeader)
            {
                case 0x20746D66: // 'fmt '
                    {
                        this.NumOfChannels = MemoryMarshal.Read<short>(chunk[2..4]);
                        this.SamplesPerSecond = MemoryMarshal.Read<int>(chunk[4..8]);
                        this.BitsPerSample = MemoryMarshal.Read<short>(chunk[14..16]);
                    }
                    break;
                case 0x61746164: // 'data'
                    {
                        if (this.NumOfChannels != 2) throw new NotSupportedException("Num of channels should be 2.");
                        if (this.BitsPerSample != 16) throw new NotSupportedException("Bits per Sample should be 16.");
                        this.Channel1 = new short[chunk.Length / 4];
                        this.Channel2 = new short[chunk.Length / 4];
                        for (int i = 0; i < this.Channel1.Length; ++i)
                        {
                            this.Channel1[i] = MemoryMarshal.Read<short>(chunk[(2 * i)..(2 * i + 2)]);
                            this.Channel2[i] = MemoryMarshal.Read<short>(chunk[(2 * i + 2)..(2 * i + 4)]);
                        }
                    }
                    break;
                default:
                    break;
            }
            position += chunkSize + 8;
        }

        if (this.Channel1 == null) throw new InvalidOperationException("Buffer has no 'data' chunk.");
    }

    /// <summary>
    /// 1サンプルあたりのビット数
    /// </summary>
    public int BitsPerSample { get; }
    /// <summary>
    /// サンプリング周波数
    /// </summary>
    public int SamplesPerSecond { get; }
    /// <summary>
    /// チャンネル数
    /// </summary>
    public int NumOfChannels { get; }

    /// <summary>
    /// チャンネル1のバイト列
    /// </summary>
    public short[] Channel1 { get; }
    /// <summary>
    /// チャンネル2のバイト列
    /// </summary>
    public short[]? Channel2 { get; }


    /// <summary>
    /// 指定したWAVファイルのパスから<see cref="Wav"/>クラスの新しいインスタンスを作成する
    /// </summary>
    /// <param name="filepath">WAVファイルのパス</param>
    /// <returns><see cref="Wav"/>クラスの新しいインスタンス</returns>
    public static Wav FromWavFile(string filepath)
    {
        using FileStream fileStream = new(filepath, FileMode.Open, FileAccess.Read);
        byte[] buffer = new byte[fileStream.Length];
        fileStream.Read(buffer, 0, buffer.Length);
        return new Wav(buffer);
    }

    /// <summary>
    /// 指定したMP3ファイルのパスから<see cref="Wav"/>クラスの新しいインスタンスを作成する
    /// </summary>
    /// <param name="filepath">Mp3ファイルのパス</param>
    /// <returns><see cref="Wav"/>クラスの新しいインスタンス</returns>
    public static async Task<Wav> FromMp3File(string filepath)
    {
        return new Wav(await Mp3.Mp3FileToWavBuffer(filepath));
    }
}
