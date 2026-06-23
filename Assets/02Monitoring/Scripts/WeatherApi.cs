using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class WeatherApi : MonoBehaviour
{
    [SerializeField] private string apiKey = "2e678cdb311e32190ed39b59abd476d9";
    [SerializeField] private string city = "Seoul";
    public TMP_Text weatherText;

    public Image weatherIcon;           // 날씨 아이콘 이미지 컴포넌트
    public Sprite iconClear;            // 맑음
    public Sprite iconClouds;           // 구름
    public Sprite iconRain;             // 비
    public Sprite iconDrizzle;          // 이슬비
    public Sprite iconThunderstorm;     // 천둥번개
    public Sprite iconSnow;             // 눈
    public Sprite iconFog;              // 안개

    void Start()
    {
        StartCoroutine(GetWeatherInfo());
    }

    IEnumerator GetWeatherInfo()
    {
        string url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={apiKey}&units=metric";

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Weather API Error : {webRequest.error}");
                if (weatherText != null)
                {
                    weatherText.text = "날씨 정보 로드 실패";
                }
            }
            else
            {
                ProcessWeatherInfo(webRequest.downloadHandler.text);
            }
        }
    }

    private void ProcessWeatherInfo(string jsonData)
    {
        WeatherData weatherData = JsonUtility.FromJson<WeatherData>(jsonData);

        if (weatherData == null || weatherData.weather == null || weatherData.weather.Length == 0 || weatherData.main == null)
        {
            Debug.LogError("Weather API JSON 파싱 에러");
            if (weatherText != null)
            {
                weatherText.text = "날씨 정보 파싱 실패";
            }
            return;
        }

        string main = weatherData.weather[0].main;
        float temp = Mathf.Round(weatherData.main.temp);

        if (weatherText != null)
        {
            weatherText.text = $"{TranslateWeather(main)} {temp}°C";
        }
        if (weatherIcon != null)
        {
            weatherIcon.sprite = GetWeatherSprite(main);
        }
    }

    private Sprite GetWeatherSprite(string main)
    {
        return main switch
        {
            "Clear" => iconClear,
            "Clouds" => iconClouds,
            "Rain" => iconRain,
            "Drizzle" => iconDrizzle,
            "Thunderstorm" => iconThunderstorm,
            "Snow" => iconSnow,
            "Mist" => iconFog,
            "Fog" => iconFog,
            "Haze" => iconFog,
            _ => iconClouds
        };
    }

    private string TranslateWeather(string main)
    {
        return main switch
        {
            "Clear" => "맑음",
            "Clouds" => "구름",
            "Rain" => "비",
            "Drizzle" => "이슬비",
            "Thunderstorm" => "천둥번개",
            "Snow" => "눈",
            "Mist" => "안개",
            "Fog" => "짙은 안개",
            "Haze" => "실안개",
            "Dust" => "황사",
            "Sand" => "모래바람",
            _ => main  // 알 수 없는 날씨명은 그대로 노출
        };
    }

    [System.Serializable]
    public class WeatherData
    {
        public Coord coord;
        public Weather[] weather;
        public string baseInfo;
        public Main main;
        public int visibility;
        public Wind wind;
        public Rain rain;
        public Clouds clouds;
        public long dt;
        public Sys sys;
        public int timezone;
        public int id;
        public string name;
        public int cod;
    }

    [System.Serializable]
    public class Coord
    {
        public float lon;
        public float lat;
    }

    [System.Serializable]
    public class Weather
    {
        public int id;
        public string main;
        public string description;
        public string icon;
    }

    [System.Serializable]
    public class Main
    {
        public float temp;
        public float feels_like;
        public float temp_min;
        public float temp_max;
        public int pressure;
        public int humidity;
        public int sea_level;
        public int grnd_level;
    }

    [System.Serializable]
    public class Wind
    {
        public float speed;
        public int deg;
        public float gust;
    }

    [System.Serializable]
    public class Rain
    {
        public float _1h; // 강수량
    }

    [System.Serializable]
    public class Clouds
    {
        public int all;
    }

    [System.Serializable]
    public class Sys
    {
        public int type;
        public int id;
        public string country;
        public long sunrise;
        public long sunset;
    }
}
