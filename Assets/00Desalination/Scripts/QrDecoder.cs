using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using ZXing;
using ZXing.Common;

/// <summary>
/// 카메라 픽셀(Color32 배열)에서 QR 코드를 디코드하는 순수 헬퍼. MonoBehaviour 아님.
/// ZXing.Net(NuGetForUnity 로 설치, Assets/Packages/ZXing.Net.*) 사용.
/// </summary>
public static class QrDecoder
{
    private static readonly BarcodeReaderGeneric Reader = new BarcodeReaderGeneric
    {
        AutoRotate = false,
        Options = new DecodingOptions
        {
            TryHarder = true,
            PossibleFormats = new List<BarcodeFormat> { BarcodeFormat.QR_CODE },
        }
    };

    /// <summary>
    /// 패스스루 카메라의 픽셀 버퍼에서 QR 문자열을 찾아본다. 못 찾으면 false, text 는 null.
    /// </summary>
    /// <param name="flipVertical">
    /// Unity 텍스처(bottom-left 원점)와 ZXing(top-left 원점) 기준이 달라 상하 반전이 필요할 수 있음.
    /// 실기기에서 스캔이 안 되면 이 값을 반대로 바꿔서 테스트해볼 것.
    /// </param>
    public static bool TryDecode(NativeArray<Color32> colors, int width, int height, bool flipVertical, out string text)
    {
        text = null;

        if (!colors.IsCreated || width <= 0 || height <= 0 || colors.Length < width * height)
        {
            return false;
        }

        byte[] rgba = new byte[colors.Length * 4];
        for (int i = 0; i < colors.Length; i++)
        {
            Color32 c = colors[i];
            int o = i * 4;
            rgba[o] = c.r;
            rgba[o + 1] = c.g;
            rgba[o + 2] = c.b;
            rgba[o + 3] = c.a;
        }

        if (flipVertical)
        {
            rgba = FlipVertically(rgba, width, height);
        }

        Result result;
        try
        {
            result = Reader.Decode(rgba, width, height, RGBLuminanceSource.BitmapFormat.RGBA32);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"QR 디코드 중 예외: {e.Message}");
            return false;
        }

        if (result == null || string.IsNullOrEmpty(result.Text))
        {
            return false;
        }

        text = result.Text;
        return true;
    }

    private static byte[] FlipVertically(byte[] rgba, int width, int height)
    {
        int stride = width * 4;
        byte[] flipped = new byte[rgba.Length];
        for (int y = 0; y < height; y++)
        {
            Array.Copy(rgba, y * stride, flipped, (height - 1 - y) * stride, stride);
        }
        return flipped;
    }
}
