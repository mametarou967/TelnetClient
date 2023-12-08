using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelnetClient.Model.SerialInterfaceProtocol
{
    public interface ICommand
    {
        CommandType CommandType { get; }

        byte[] ByteArray();

        string ToString();
    }
}
