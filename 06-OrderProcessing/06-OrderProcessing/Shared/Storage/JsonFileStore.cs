using _06_OrderProcessing.Shared.AppLogger;
using _06_OrderProcessing.Shared.Exceptions;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace _06_OrderProcessing.Shared.Storage;
public class JsonFileStore
{ 
    private readonly string _dataDirectory =
    Path.Combine(AppContext.BaseDirectory, "Data");

    private static readonly Dictionary<string,SemaphoreSlim> _semaphores = new();
    private static readonly object _semaphoresLock = new();

    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

   
    private SemaphoreSlim GetSemaphore(string fileName)
    {
        string fullPath = Path.GetFullPath(GetFilePath(fileName));

        lock (_semaphoresLock)
        {
            if(!_semaphores.TryGetValue(fullPath, out var semaphore))
            {
                semaphore = new SemaphoreSlim(1, 1);
                _semaphores.Add(fullPath, semaphore);
            }

            return semaphore;
        }
    }
    private string GetFilePath(string fileName)
    {
        return Path.Combine(_dataDirectory, fileName);
    }

    #region Synchronous Methods

    // Synchronous Read
    public List<T> Read<T>(string fileName)
    {
        SemaphoreSlim semaphore = GetSemaphore(fileName);
        semaphore.Wait();

        try
        {
            string filePath = GetFilePath(fileName);

            if (!File.Exists(filePath))
            {
                return new List<T>();
            }

            try
            {
                string json = File.ReadAllText(filePath);

                return JsonSerializer.Deserialize<List<T>>(json, _options)
                       ?? new List<T>();
            }
            catch (JsonException ex)
            {
                throw new DataFileCorruptedException(fileName, ex);
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    // Synchronous Write
    public void Write<T>(string fileName, List<T> data)
    {
        SemaphoreSlim semaphore = GetSemaphore(fileName);
        semaphore.Wait();

        try
        {
            string filePath = GetFilePath(fileName);

            Directory.CreateDirectory(_dataDirectory);

            string tempPath = filePath + ".tmp";
            string json = JsonSerializer.Serialize(data, _options);

            try
            {
                File.WriteAllText(tempPath, json, System.Text.Encoding.UTF8);
                File.Move(tempPath, filePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }
        finally
        {
            semaphore.Release();
        }
    }



    #endregion

    public async Task<List<T>> ReadAsync<T>(string fileName, CancellationToken token)
    {
        SemaphoreSlim semaphore = GetSemaphore(fileName);

        await semaphore.WaitAsync(token);

        try
        {
            return await ReadCoreAsync<T>(fileName, token);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<List<T>> ReadCoreAsync<T>(
    string fileName,
    CancellationToken token)
    {
        string filePath = GetFilePath(fileName);

        if (!File.Exists(filePath))
        {
            return new List<T>();
        }

        try
        {
            string json = await File.ReadAllTextAsync(filePath, token);

            return JsonSerializer.Deserialize<List<T>>(json, _options)
                   ?? new List<T>();
        }
        catch (JsonException ex)
        {
            throw new DataFileCorruptedException(fileName, ex);
        }
    }

    public async Task WriteAsync<T>(string fileName, List<T> data, CancellationToken token)
    {
        SemaphoreSlim semaphore = GetSemaphore(fileName);

        await semaphore.WaitAsync(token);

        try
        {
            await WriteCoreAsync(fileName, data, token);
        }
        
        finally
        {

            semaphore.Release();
        }
        
    }

    private async Task WriteCoreAsync<T>(string fileName,List<T> data,CancellationToken token)
    {
        string filePath = GetFilePath(fileName);

        Directory.CreateDirectory(_dataDirectory);

        string tempPath = filePath + ".tmp";

        string json = JsonSerializer.Serialize(data, _options);

        try
        {
            await File.WriteAllTextAsync(tempPath, json, System.Text.Encoding.UTF8, token);

            //Console.WriteLine($"Temp path: {tempPath}");
            //Console.WriteLine($"Target path: {filePath}");

            try
            {
                //Console.WriteLine(
                //     $"Temp exists: {File.Exists(tempPath)}, " +
                //     $"Target exists: {File.Exists(filePath)}, " +
                //     $"Target is directory: {Directory.Exists(filePath)}");

                File.Move(tempPath, filePath, overwrite: true);
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Error(
                    "JsonFileStore",
                    $"Cannot replace '{filePath}'. Temp file: '{tempPath}'. Error: {ex.Message}");

                throw;
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }


    public async Task UpdateAsync<T>(string fileName, Action<List<T>> updateAction, CancellationToken token)
    {
        SemaphoreSlim semaphore = GetSemaphore(fileName);

        await semaphore.WaitAsync(token);

        try
        {
          // Read the existing data
           List<T> data = await ReadCoreAsync<T>(fileName, token);

          // Modify
           updateAction(data);

            // Write
          await WriteCoreAsync(fileName, data, token);
        }
        finally
        {
            semaphore.Release();
        }


    }
}
