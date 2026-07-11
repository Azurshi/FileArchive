using System;

namespace AzurArchive.Core;

public class RaceException: Exception {

}

public class NotInitializedException : Exception {
    public NotInitializedException() { }
    public NotInitializedException(string message) : base(message) { }
}

