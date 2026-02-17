namespace Eticaret.WebUI.ExtensionMethods;
using Newtonsoft.Json;
public static class SessionExtensionMethods
{
    public static void SetJson(this ISession session, string key, object value)
    {
        session.SetString(key, JsonConvert.SerializeObject(value));
    
    }

    public static T? GetJson<T>(this ISession session, string key)
    {
        var data = session.GetString(key);

        return data == null ? default(T) :JsonConvert.DeserializeObject<T>(data);
    }
}
