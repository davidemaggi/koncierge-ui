using System;
using System.Collections.Generic;
using System.Text;

namespace KonciergeUI.Data.Secure
{
    public interface ISecureStore
    {
        Task<string?> GetAsync(string key);
        Task SetAsync(string key, string value);
        Task<bool> RemoveAsync(string key);
        Task RemoveAllAsync();
    }
}
