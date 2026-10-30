using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class GastitisAPIManager
{
    private const string BaseUrl = "http://localhost:5036/api/";
    private const string ExpensesEndPoint = BaseUrl + "expenses";
    private const string CategoriesEndPoint = BaseUrl + "categories";
    private readonly IAccessTokenProvider _tokenProvider;

    public GastitisAPIManager(IAccessTokenProvider tokenProvider)
    {
        if(tokenProvider == null)
        {
			throw new ArgumentNullException(nameof(tokenProvider));
		}
        _tokenProvider = tokenProvider;
    }

    public async Task CreateTestCategoryAsync()
    {
        var category = new CategoryDTO
        {
            Name = "Test Category"
        };
        var created = await PostCategoryAsync(category);
        if (created != null)
        {
            Debug.Log($"Category created with ID: {created.Id} | Name: {created.Name}");
        }
    }

    public async Task<CategoryDTO> PostCategoryAsync(CategoryDTO category)
    {
        string json = JsonConvert.SerializeObject(category);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        using (var www = new UnityWebRequest(CategoriesEndPoint, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            await SendRequestAsync(www);
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error creating category: {www.error} | Response: {www.downloadHandler?.text}");
                return null;
            }
            var responseCategory = JsonConvert.DeserializeObject<CategoryDTO>(www.downloadHandler.text);
            return responseCategory;
        }
    }

    public async Task<List<CategoryDTO>> GetCategoriesAsync()
    {
        using (var www = UnityWebRequest.Get(CategoriesEndPoint))
        {
            await SendRequestAsync(www);
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error fetching categories: {www.error}");
                throw new InvalidOperationException($"Get Categories failed with code: {www.responseCode}");
            }

			List<CategoryDTO> categories;
			try
			{
				categories = JsonConvert.DeserializeObject<List<CategoryDTO>>(www.downloadHandler.text);
			}
			catch (JsonException)
			{
				throw new InvalidOperationException("Reading categories failed, content didn't come with the expected format");
			}

			if (categories == null)
			{
				throw new InvalidOperationException("Reading categories failed, data is null");
			}

			Debug.Log($"Fetched {categories.Count} categories from API.");
            foreach (var category in categories)
            {
                if(category == null)
                {
					throw new InvalidOperationException("Reading categories failed, found null category");
				}

				Debug.Log($"Category ID: {category.Id} | Name: {category.Name}");
            }
            return categories;
        }
    }

    public async Task CreateTestExpenseAsync()
    {
        var expense = new ExpenseDTO
        {
            Value = 100.0m,
            Description = "Test Expense",
            categoryId = 1,
            Date = DateTime.UtcNow
        };

        var created = await PostExpenseAsync(expense);
        if (created != null)
        {
            Debug.Log($"Expense created with ID: {created.Id} | Value: {created.Value} | Description: {created.Description}");
        }
    }

    public async Task<ExpenseDTO> PostExpenseAsync(ExpenseDTO expense)
    {
        string json = JsonConvert.SerializeObject(expense);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (var www = new UnityWebRequest(ExpensesEndPoint, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            await SendRequestAsync(www);

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error creating expense: {www.error} | Response: {www.downloadHandler?.text}");
                return null;
            }

            var responseExpense = JsonConvert.DeserializeObject<ExpenseDTO>(www.downloadHandler.text);
            return responseExpense;
        }
    }

    public async Task<ExpenseDTO> PutExpenseAsync(int id, UpdateExpenseDTO expense)
    {
        string json = JsonConvert.SerializeObject(expense);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        var endPoint = ExpensesEndPoint;
        endPoint += $"/{id}";

        using (var www = new UnityWebRequest(endPoint, "PUT"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            await SendRequestAsync(www);

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error updating expense: {www.error} | Response: {www.downloadHandler?.text}");
                return null;
            }

            var responseExpense = JsonConvert.DeserializeObject<ExpenseDTO>(www.downloadHandler.text);
            return responseExpense;
        }
    }

    public async Task<bool> DeleteExpenseAsync(int id)
    {
        var endPoint = ExpensesEndPoint;
        endPoint += $"/{id}";

        using (var www = UnityWebRequest.Delete(endPoint))
        {
            await SendRequestAsync(www);

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error DELETING expense: {www.error} | Response: {www.downloadHandler?.text}");
                return false;
            }

            return true;
        }
    }



    public async Task<ExpenseDTO> GetExpenseByID(int id)
    {
        var endPoint = ExpensesEndPoint;
        endPoint += $"/{id}";

        using (var www = UnityWebRequest.Get(endPoint))
        {
            await SendRequestAsync(www);

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error fetching expense by id {id}: {www.error}");
                return null;
            }

            var expenseFound = JsonConvert.DeserializeObject<ExpenseDTO>(www.downloadHandler.text);
            Debug.Log($"Base result from API: " + expenseFound);
            return expenseFound;
        }
    }

    public async Task<PagedResponseDTO<ExpenseDTO>> GetExpensesAsync(
        ExpenseFilterDTO filterRequest = null, 
        ExpenseSortingDTO sortingRequest = null)
    {

        var endPoint = ExpensesEndPoint;
        endPoint = AddFilterOptions(endPoint, filterRequest);
        endPoint = AddSortingOptions(endPoint, sortingRequest);

        using (var www = UnityWebRequest.Get(endPoint))
        {
            await SendRequestAsync(www);

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error fetching expenses: {www.error}");
                throw new InvalidOperationException($"Get Expenses failed with code: {www.responseCode}");
            }

            PagedResponseDTO<ExpenseDTO> expensesPagedResponse;
			try
            {
				expensesPagedResponse = JsonConvert.DeserializeObject<PagedResponseDTO<ExpenseDTO>>(www.downloadHandler.text);
			}
            catch (JsonException)
            {
                throw new InvalidOperationException("Reading expenses failed, content didn't come with the expected format");
            }

			if (expensesPagedResponse == null)
			{
				throw new InvalidOperationException("Reading Expenses failed, data is null");
			}

			Debug.Log($"Base result from API: " + expensesPagedResponse);
            Debug.Log($"Fetched {expensesPagedResponse.TotalCount} expenses from API.");

			if (expensesPagedResponse.Items == null)
			{
				throw new InvalidOperationException("Reading Expenses failed, found null expenses items");
			}
			foreach (var expense in expensesPagedResponse.Items)
			{
				if (expense == null)
				{
					throw new InvalidOperationException("Reading Expenses failed, found null expense");
				}

				Debug.Log($"Expense ID: {expense.Id} | Value: {expense.Value} | Description: {expense.Description}");
            }

            return expensesPagedResponse;
        }
    }

    public async Task<ExpensesSummaryDTO> GetExpensesSummaryAsync(
        ExpenseFilterDTO filterRequest = null, 
        ExpenseSortingDTO sortingRequest = null)
    {

        var endPoint = ExpensesEndPoint;
        endPoint += "/Summary";
        endPoint = AddFilterOptions(endPoint, filterRequest);

        using (var www = UnityWebRequest.Get(endPoint))
        {
            await SendRequestAsync(www);

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error fetching expenses: {www.error}");
                return null;
            }

            var expensesPagedResponse = JsonConvert.DeserializeObject<ExpensesSummaryDTO>(www.downloadHandler.text);
            Debug.Log($"Base result from API: " + expensesPagedResponse);
            return expensesPagedResponse;
        }
    }

    public async Task<PagedResponseDTO<ExpensesCategorySummaryResponseDTO>> GetCategorySummaryAsync(
        ExpenseFilterDTO filterRequest = null)
    {
        var endPoint = ExpensesEndPoint;
        endPoint += "/CategorySummary";
        endPoint = AddFilterOptions(endPoint, filterRequest);

        using (var www = UnityWebRequest.Get(endPoint))
        {
            await SendRequestAsync(www);

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error fetching expenses: {www.error}");
                return null;
            }

            var expensesPagedResponse = 
                JsonConvert.DeserializeObject<PagedResponseDTO<ExpensesCategorySummaryResponseDTO>>(www.downloadHandler.text);
            Debug.Log($"Base result from API: " + expensesPagedResponse);
            return expensesPagedResponse;
        }
    }

    private string AddFilterOptions(string endPoint, ExpenseFilterDTO filter)
    {
        var baseEndpoint = endPoint;
        if (filter == null) return baseEndpoint;

        baseEndpoint += "?";

        baseEndpoint += $"page={filter.Page}";
        baseEndpoint += $"&pageSize={filter.PageSize}";

        if (filter.Year.HasValue) baseEndpoint += $"&year={filter.Year}";
        if (filter.Month.HasValue) baseEndpoint += $"&month={filter.Month}";
        if (filter.CategoryID.HasValue) baseEndpoint += $"&categoryID={filter.CategoryID}";
        if (!string.IsNullOrEmpty(filter.Keyword)) baseEndpoint += $"&Keyword={filter.Keyword}";

        return baseEndpoint;
    }

    private string AddSortingOptions(string endPoint, ExpenseSortingDTO sorting)
    {
        var baseEndpoint = endPoint;
        if (sorting == null) return baseEndpoint;

        //In case a filter was already applied
        var separator = endPoint.Contains('?')? "&": "?";

        baseEndpoint += $"{separator}{nameof(SortBy)}={sorting.SortBy}";
        baseEndpoint += $"&{nameof(SortDirection)}={sorting.SortDirection}";
        return baseEndpoint;
    }

    private void ValidateCurrentSessionVersion(long originalSessionvVersion)
    {
        if(_tokenProvider.SessionVersion != originalSessionvVersion)
        {
            throw new OperationCanceledException("The authentication session changed during the request");
        }
    }

    private async Task SendRequestAsync(UnityWebRequest request)
    {
        var originalSessionVersion = _tokenProvider.SessionVersion;

        try
        {
            string accessToken = await _tokenProvider.GetAccessTokenAsync();
            ValidateCurrentSessionVersion(originalSessionVersion);

            request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            request.redirectLimit = 0;
            request.timeout = 30;

            var completion = new TaskCompletionSource<bool>();
            var operation = request.SendWebRequest();

            operation.completed += _ => completion.TrySetResult(true);

            if (operation.isDone)
            {
                completion.TrySetResult(true);
            }
            await completion.Task;

        }
        catch (Exception) when (originalSessionVersion != _tokenProvider.SessionVersion)
        {
            throw new OperationCanceledException("The authentication session changed during the request");
        }

        ValidateCurrentSessionVersion(originalSessionVersion);
    }
}
