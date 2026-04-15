using UnityEngine.Networking;

public class IgnoreSSL : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}