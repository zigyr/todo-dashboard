using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace TodoDashboard.Services;

/// <summary>
/// DeepSeek Key 的读取顺序：
/// 1. 环境变量 DEEPSEEK_API_KEY（开发/测试）
/// 2. Windows 凭据管理器（正式使用）
/// 不会写进项目文件。
/// </summary>
public sealed class ApiKeyStore
{
    private const string Target = "TodoDashboard/DeepSeekApiKey";
    private const int CredentialTypeGeneric = 1;
    private const int CredentialPersistLocalMachine = 2;
    private const int ErrorNotFound = 1168;

    public string? GetKey()
    {
        var fromEnvironment = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment.Trim();
        }

        if (!CredReadW(Target, CredentialTypeGeneric, 0, out var pointer))
        {
            return null;
        }

        try
        {
            var credential = Marshal.PtrToStructure<Credential>(pointer);
            if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize <= 0)
            {
                return null;
            }

            return Marshal
                .PtrToStringUni(credential.CredentialBlob, credential.CredentialBlobSize / 2)
                ?.Trim();
        }
        finally
        {
            CredFree(pointer);
        }
    }

    public void SaveKey(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API Key 不能为空。", nameof(apiKey));
        }

        var key = apiKey.Trim();
        var blob = Marshal.StringToCoTaskMemUni(key);

        try
        {
            var credential = new Credential
            {
                Type = CredentialTypeGeneric,
                TargetName = Target,
                Comment = "DeepSeek API Key for Todo Dashboard",
                CredentialBlob = blob,
                CredentialBlobSize = Encoding.Unicode.GetByteCount(key),
                Persist = CredentialPersistLocalMachine,
                UserName = "TodoDashboard",
            };

            if (!CredWriteW(ref credential, 0))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error(),
                    "保存 API Key 到 Windows 凭据管理器失败。");
            }
        }
        finally
        {
            Marshal.FreeCoTaskMem(blob);
        }
    }

    public void DeleteKey()
    {
        if (!CredDeleteW(Target, CredentialTypeGeneric, 0))
        {
            var error = Marshal.GetLastWin32Error();
            if (error != ErrorNotFound)
            {
                throw new Win32Exception(error, "删除 API Key 失败。");
            }
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct Credential
    {
        public int Flags;
        public int Type;
        public string TargetName;
        public string Comment;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
        public int CredentialBlobSize;
        public IntPtr CredentialBlob;
        public int Persist;
        public int AttributeCount;
        public IntPtr Attributes;
        public string TargetAlias;
        public string UserName;
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredWriteW(ref Credential credential, int flags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredReadW(
        string target,
        int type,
        int flags,
        out IntPtr credential);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CredDeleteW(string target, int type, int flags);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern void CredFree(IntPtr buffer);
}
