using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Maui.Storage;

namespace KonciergeUI.Data.Secure
{
    public class SecureStore : ISecureStore
    {
        public async Task<string?> GetAsync(string key)
        {
            try
            {
                return await SecureStorage.GetAsync(key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to get secure storage key '{key}': {ex.Message}");
                return null;
            }
        }

        public async Task SetAsync(string key, string value)
        {
            try
            {
                await SecureStorage.SetAsync(key, value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to set secure storage key '{key}': {ex.Message}");
            }
        }

        public Task<bool> RemoveAsync(string key)
        {
            try
            {
                return Task.FromResult(SecureStorage.Remove(key));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to remove secure storage key '{key}': {ex.Message}");
                return Task.FromResult(false);
            }
        }

        public Task RemoveAllAsync()
        {
            try
            {
                SecureStorage.RemoveAll();
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to clear secure storage: {ex.Message}");
                return Task.CompletedTask;
            }
        }
    }
}
