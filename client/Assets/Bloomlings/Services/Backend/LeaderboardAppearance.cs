using System;
using System.Collections;
using System.Globalization;
using System.Text;
using Bloomlings.Client.Meta.Profile;
using Bloomlings.Client.Services.Save;
using Bloomlings.Content.Packs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bloomlings.Client.Services.Backend
{
    /// <summary>Cosmetic identifiers, never baked portraits. Old rows without metadata keep a neutral fallback.</summary>
    public sealed record LeaderboardAppearance(string? Avatar, string? Frame, string? Badge, string? Marker, string? Name = null)
    {
        public static LeaderboardAppearance FromSave(PlayerSave save)
        {
            string? Slot(string kind) => save.Cosmetics.Equipped.TryGetValue("profile."+kind,out string? value)?value:null;
            AvatarItem? avatar=AvatarCatalog.Get(Slot("avatar"));
            string id=avatar!=null&&(avatar.IsFree||save.Cosmetics.Owned.Contains(avatar.Id))?avatar.Id:AvatarCatalog.DefaultId;
            return new LeaderboardAppearance(id,Slot("frame"),Slot("badge"),Slot("marker"),ProfileService.CleanName(save.Profile.Name));
        }
        public string ToJson()=>new JObject{["avatar"]=Avatar,["frame"]=Frame,["badge"]=Badge,["marker"]=Marker,["name"]=Name}.ToString(Formatting.None);
        public long Fingerprint => (long)(ulong.Parse(PackIntegrity.ComputeSha256Hex(Encoding.UTF8.GetBytes(ToJson())).Substring(0,16),NumberStyles.HexNumber,CultureInfo.InvariantCulture)&long.MaxValue);
        public static LeaderboardAppearance? FromMetadata(object? metadata)
        {
            if(metadata==null)return null;
            try
            {
                JToken token=metadata is string json?JToken.Parse(json):metadata as JToken??JToken.FromObject(metadata);
                if(token is not JObject root||root["appearance"] is not JObject look)return null;
                string? Read(string key)
                {
                    string? value=look[key]?.Type==JTokenType.String?(string?)look[key]:null;
                    return value!=null&&value.Length<=128?value:null;
                }
                return new LeaderboardAppearance(Read("avatar"),Read("frame"),Read("badge"),Read("marker"),Read("name"));
            }
            catch(JsonException){return null;}
        }
    }
    /// <summary>Optional capability keeps old/offline providers compatible; a same-level call changes cosmetics only.</summary>
    public interface ILeaderboardAppearanceService
    {
        IEnumerator SubmitAppearance(int level,int contentVersion,string commandLogHash,LeaderboardAppearance appearance,Action<SubmitResult> done);
    }
}
