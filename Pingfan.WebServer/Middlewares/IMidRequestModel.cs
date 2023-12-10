namespace Pingfan.WebServer.Middlewares;

public interface IMidRequestModel
{
    /// <summary>
    /// 检查是否符合要求
    /// </summary>
    void Check();
}