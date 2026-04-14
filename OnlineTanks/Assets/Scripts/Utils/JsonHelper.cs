using UnityEngine;

public static class JsonHelper
{
    [System.Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }

    public static T[] FromJson<T>(string json)
    {
        string newJson = "{ \"Items\": " + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.Items;
    }
}