# A Batching Challenge

Solving a challenge in [.NET](https://dotnet.microsoft.com/en-us/learn/dotnet/what-is-dotnet).

- Taking items of a message queue. Consider that there is a some processing that is required to messages, that may take a little while. However we would not want to block receiving messages while that long processing occurs.
- Once the long processing of the messages has happened, the messages need to be forwarded on. Assume there will be more than one at this point so send as many as can (i.e. batching).
