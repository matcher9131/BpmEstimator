using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;

namespace BpmEstimator;

/// <summary>
/// MP3ファイルに関するユーティリティクラス
/// </summary>
public static class Mp3
{
    private static async Task<byte[]> StreamToByteArray(IRandomAccessStream stream)
    {
        byte[] buffer = new byte[stream.Size];
        await stream.ReadAsync(buffer.AsBuffer(), (uint)stream.Size, InputStreamOptions.None);
        return buffer;
    }

    /// <summary>
    /// Mp3ファイルをデコードしてWAVファイル形式のバイト列を得る
    /// </summary>
    /// <param name="filepath">MP3ファイルのパス</param>
    /// <returns>WAVファイル形式のバイト列</returns>
    public static async Task<byte[]> Mp3FileToWavBuffer(string filepath)
    {
        StorageFile srcFile = await StorageFile.GetFileFromPathAsync(filepath);
        using IRandomAccessStream source = await srcFile.OpenAsync(FileAccessMode.Read);
        InMemoryRandomAccessStream destination = new();
        MediaEncodingProfile profile = MediaEncodingProfile.CreateWav(AudioEncodingQuality.Medium);
        MediaTranscoder transcoder = new();
        PrepareTranscodeResult prepareTranscodeResult = await transcoder.PrepareStreamTranscodeAsync(source, destination.CloneStream(), profile);
        await prepareTranscodeResult.TranscodeAsync();
        return await StreamToByteArray(destination);
    }
    
}
