// Copyright © 2025 Always Active Technologies PTY Ltd

using TechAptV1.Client.Models;

namespace TechAptV1.Client.Services;

/// <summary>
/// Default constructor providing DI Logger and Data Service
/// </summary>
/// <param name="logger"></param>
/// <param name="dataService"></param>
public sealed class ThreadingService(ILogger<ThreadingService> logger, DataService dataService)
{
    private int _oddNumbers = 0;
    private int _evenNumbers = 0;
    private int _primeNumbers = 0;
    private int _totalNumbers = 0;
    private readonly Random _rand = new();
    private const int MaxEntries = 10000000;
    private const int InitialEntries = 2500000;
    private readonly List<int> _numbers = new();
    private static readonly object _lock = new();
    private bool _isRunning = false;

    public int GetOddNumbers() => _oddNumbers;
    public int GetEvenNumbers() => _evenNumbers;
    public int GetPrimeNumbers() => _primeNumbers;
    public int GetTotalNumbers() => _totalNumbers;

    /// <summary>
    /// Start the random number generation process
    /// </summary>
    public async Task Start()
    {
        if (_isRunning) // Prevent multiple runs
        {
            return;
        }

        // Check if there are existing values if there are, reset them
        // just checking total numbers instead of odd, even and prime
        if (_totalNumbers > 0)
        {
            logger.LogInformation($"Previous computation detected - Odd: {_oddNumbers}, Even: {_evenNumbers}, Prime: {_primeNumbers}, Total: {_totalNumbers}");

            // Reset before starting a new computation
            logger.LogInformation("Resetting values...");
            ResetCount();
        }

        _isRunning = true;
        logger.LogInformation("Start");

        var oddThread = new Thread(GenerateOddNumbers);
        var primeThread = new Thread(GeneratePrimeNumbers);
        oddThread.Start();
        primeThread.Start();

        while (_numbers.Count < MaxEntries) // Stop condition in loop
        {
            lock (_lock)
            {
                if (_numbers.Count >= InitialEntries)
                {
                    Task.Run(GenerateEvenNumbers); // More efficient than Thread.Start()
                }
            }
            //I notice that the CPU usage is high when the number of threads is high
            //Insert a delay to prevent CPU overload
            //Since there is no sleep or delay, the CPU works at a higher usage, even though it’s just waiting for a condition to be met.
            await Task.Delay(100); // Prevent excessive CPU usage
        }
        logger.LogInformation("Max limit reached. Stopping tasks...");
        // Sorting list before displaying final results
        lock (_lock)
        {
            _numbers.Sort();
        }
        //Display the total count of numbers, odd numbers, and even numbers.
        string message = $"Final Count - Total: {_totalNumbers}, Odd: {_oddNumbers}, Even: {_evenNumbers}";
        logger.LogInformation(message);
        _isRunning = false;
    }

    /// <summary>
    /// Persist the results to the SQLite database
    /// </summary>
    public async Task Save()
    {
        try
        {
            if (!_isRunning && _numbers.Count > 0)
            {
                logger.LogInformation("Save");
                var numberList = _numbers.Select(n => new Number { Value = n }).ToList();
                await dataService.Save(numberList);
                logger.LogInformation("Data saved successfully.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"An error occurred while saving data. Error: {ex.Message}");
        }
    }


    /// <summary>
    /// Fetch data from the SQLite database
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public async Task<List<Number>> Get(int count)
    {
        var response = await dataService.Get(count);
        return response.ToList();
    }


    /// <summary>
    /// Fetch data from the SQLite database
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public async Task<List<Number>> GetAll()
    {
        var response = await dataService.GetAll();
        return response.ToList();
    }

    /// <summary>
    /// Reset all values to their initial state
    /// </summary>
    public void ResetCount()
    {
        lock (_lock)
        {
            _oddNumbers = 0;
            _evenNumbers = 0;
            _primeNumbers = 0;
            _totalNumbers = 0;
            _numbers.Clear();
            _isRunning = false;
        }
        logger.LogInformation("ThreadingService has been reset.");
    }

    #region private methods

    private void GenerateOddNumbers()
    {
        while (_numbers.Count < MaxEntries)
        {
            int number = _rand.Next(1, 1000000) * 2 + 1; // Generate odd number
            lock (_lock) // Ensure thread safety
            {
                if (_numbers.Count >= MaxEntries)
                {
                    break;
                }
                _numbers.Add(number);
                _oddNumbers++;
                _totalNumbers++;
            }
        }
    }

    private void GeneratePrimeNumbers()
    {
        while (_numbers.Count < MaxEntries)
        {
            int number = _rand.Next(1, 1000000);
            if (IsPrime(number))
            {
                lock (_lock)
                {
                    if (_numbers.Count >= MaxEntries)
                    {
                        break;
                    }
                    _numbers.Add(-number);
                    _primeNumbers++;
                    _totalNumbers++;
                }
            }
        }
    }

    private void GenerateEvenNumbers()
    {
        while (_numbers.Count < MaxEntries)
        {
            int number = _rand.Next(1, 1000000) * 2; // Generate even number
            lock (_lock) // Ensures thread safety
            {
                if (_numbers.Count >= MaxEntries)
                {
                    break;
                }
                _numbers.Add(number);
                _evenNumbers++;
                _totalNumbers++;
            }
        }
    }

    private static bool IsPrime(int number)
    {
        if (number <= 1)
        {
            return false;
        }
        if (number <= 3)
        {
            return true;
        }
        if (number % 2 == 0 || number % 3 == 0)
        {
            return false;
        }

        for (int i = 5; i * i <= number; i += 6)
        {
            if (number % i == 0 || number % (i + 2) == 0)
            {
                return false;
            }
        }
        return true;
    }

    #endregion
}
