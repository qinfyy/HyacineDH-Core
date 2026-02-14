using HyacineCore.Server.Database.Account;
using HyacineCore.Server.WebServer.Objects;
using Microsoft.AspNetCore.Mvc;
using static HyacineCore.Server.WebServer.Objects.LoginResJson;

namespace HyacineCore.Server.WebServer.Handler;

public class TokenLoginHandler
{
    public JsonResult Handle(string uid, string token)
    {
        var account = AccountData.GetAccountByUid(int.Parse(uid));
        var res = new LoginResJson();
        if (account == null || !account?.DispatchToken?.Equals(token) == true)
        {
            res.retcode = -201;
            res.message = "Game account cache information error";
        }
        else
        {
            res.message = "OK";
            res.data = new VerifyData(account!.Uid.ToString(), account.Username + "@egglink.me", token);
            res.data.account.name = account.Username;
            res.data.account.is_email_verify = "1";
        }

        return new JsonResult(res);
    }
}
