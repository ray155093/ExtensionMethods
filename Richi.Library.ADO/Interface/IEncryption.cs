using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Richi.Library.ADO
{
    public interface IEncryption
    {
        string Decrypt(string decryptStr);
    }
}
