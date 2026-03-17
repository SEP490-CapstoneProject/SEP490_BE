namespace Connection.Application.DTOs;

public class CreateConnectionRequest
{
    public int UserIdFrom { get; set; }
    public int UserIdTo { get; set; }
    public int ProfileId { get; set; }
}
