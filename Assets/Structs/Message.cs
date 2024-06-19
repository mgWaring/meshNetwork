using System;

public struct Message
{
    public Guid destination;

    public Guid uID;

    public string body;

    public float ttl;

    public int recentHops;

    public Message(Guid uID, Guid destination, string body, float ttl)
    {
        this.uID = uID;
        this.destination = destination;
        this.body = body;
        this.ttl = ttl;
        recentHops = 0;
    }
}
