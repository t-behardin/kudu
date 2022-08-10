using Microsoft.AspNetCore.DataProtection;
#if NETFRAMEWORK
using Microsoft.Azure.Web.DataProtection;
#endif
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace Kudu.Core.Infrastructure
{
    public static class SecurityUtility
    {
        private const string DefaultProtectorPurpose = "function-secrets";

        private static string GenerateSecretString()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] data = new byte[40];
                rng.GetBytes(data);
                string secret = Convert.ToBase64String(data);
                // Replace pluses as they are problematic as URL values
                return secret.Replace('+', 'a');
            }
        }

        public static Tuple<string, string>[] GenerateSecretStringsKeyPair(int number)
        {
            var unencryptedToEncryptedKeyPair = new Tuple<string, string>[number];
#if NETFRAMEWORK
            var protector = Microsoft.Azure.Web.DataProtection.DataProtectionProvider.CreateAzureDataProtector().CreateProtector(DefaultProtectorPurpose);
#else
            // TODO: What key should be in the Create() part?
            var protector = DataProtectionProvider.Create("azure").CreateProtector(DefaultProtectorPurpose);
#endif
            for (int i = 0; i < number; i++)
            {
                string unencryptedKey = GenerateSecretString();
                // TODO: FIX THIS BECAUSE IT FEELS SO WEIRD Encoding.Default.GetString(protector.Protect(Encoding.ASCII.GetBytes(unencryptedKey)))

                unencryptedToEncryptedKeyPair[i] = new Tuple<string, string>(unencryptedKey, protector.Protect(unencryptedKey));
            }
            return unencryptedToEncryptedKeyPair;
        }

        public static string DecryptSecretString(string content)
        {
            try
            {
#if NETFRAMEWORK
                var protector = Microsoft.Azure.Web.DataProtection.DataProtectionProvider.CreateAzureDataProtector().CreateProtector(DefaultProtectorPurpose);
#else
                // TODO: What key should be in the Create() part?
                var protector = DataProtectionProvider.Create("azure").CreateProtector(DefaultProtectorPurpose);
#endif
                return protector.Unprotect(content);
            }
            catch (CryptographicException ex)
            {
                throw new FormatException($"unable to decrypt {content}, the key is either invalid or malformed", ex);
            }
        }

        public static string GenerateFunctionToken()
        {
            string siteName = ServerConfiguration.GetApplicationName();
            string issuer = $"https://{siteName}.scm.azurewebsites.net";
            string audience = $"https://{siteName}.azurewebsites.net/azurefunctions";

            var tokenHandler = new JwtSecurityTokenHandler();
            var claimsIdentity = new ClaimsIdentity();

            string xpath = string.Format(CultureInfo.InvariantCulture, "configuration/location[@path='{0}']/system.web/machineKey/@decryptionKey", siteName);
            //XDocument d = XDocument.Parse(xml);
            //var key = ((IEnumerable)XDocument.XPathEvaluate(xpath)).Cast<XAttribute>().FirstOrDefault()?.Value;
            


            // TODO: THIS NEEDS TO CHANGE. THIS IS NOT THE DOCUMENT TO EVALUATE
            XDocument xdoc = XDocument.Parse(xpath);
            var key = ((IEnumerable)xdoc.XPathEvaluate(xpath)).Cast<XAttribute>().FirstOrDefault()?.Value;


            var signingCredentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256Signature);


            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claimsIdentity,
                Expires = DateTime.UtcNow.AddMinutes(5),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = signingCredentials

            };

            return tokenHandler.CreateToken(tokenDescriptor).ToString();

        }
    }
}

