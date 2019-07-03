using System;
public class TokenBindingOutput : IDisposable
{
    public string outputToken { get; set; }

    // Dispose method needed for implicit use of Token Store binding 
    public void Dispose()
    {

    }
}