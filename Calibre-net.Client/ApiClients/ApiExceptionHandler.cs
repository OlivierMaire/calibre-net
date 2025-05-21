namespace Calibre_net.Client.ApiClients;



// [SingletonRegistration]
public partial class BaseApiClient
{

    public T HandleApiException<T>(ApiException ex)
    {
        if (ex.StatusCode == 401)
        {
            // Handle unauthorized access, e.g., redirect to login page or show a message
            // _navigationManager.NavigateTo("/login");
            return default(T)!;
        }
        else if (ex.StatusCode == 403)
        {
            // Handle forbidden access, e.g., show an error message
            // _navigationManager.NavigateTo("/forbidden");
            return default(T)!;
        }
        else if (ex.StatusCode == 404)
        {
            // Handle not found, e.g., show a 404 page or message
            // _navigationManager.NavigateTo("/404");
            return default(T)!;
        }
        else
        {
            // Handle other status codes as needed
            throw ex;
        }
    }

  public void HandleApiException(ApiException ex)
    {
        if (ex.StatusCode == 401)
        {
            // Handle unauthorized access, e.g., redirect to login page or show a message
            // _navigationManager.NavigateTo("/login");
            return ;
        }
        else if (ex.StatusCode == 403)
        {
            // Handle forbidden access, e.g., show an error message
            // _navigationManager.NavigateTo("/forbidden");
            return ;
        }
        else if (ex.StatusCode == 404)
        {
            // Handle not found, e.g., show a 404 page or message
            // _navigationManager.NavigateTo("/404");
            return ;
        }
        else
        {
            // Handle other status codes as needed
            throw ex;
        }
    }
}