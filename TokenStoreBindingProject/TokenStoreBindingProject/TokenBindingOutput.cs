using System;
public class TokenBindingOutput : IDisposable // Use this output type for including more information (currently not used, output token is just a string) 
{
    public string outputToken { get; set; }

    // Dispose method needed for implicit use of Token Store binding 
    public void Dispose()
    {

    }
}