namespace WebApi.MinimalApi.Domain;

public class UserEntity
{
    public UserEntity()
    {
        Id = Guid.Empty;
    }

    public UserEntity(Guid id)
    {
        Id = id;
    }

    public UserEntity(Guid id, string login, string lastName, string firstName, int gamesPlayed, Guid? currentGameId)
    {
        Id = id;
        Login = login;
        LastName = lastName;
        FirstName = firstName;
        GamesPlayed = gamesPlayed;
        CurrentGameId = currentGameId;
    }

    public Guid Id
    {
        get;
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local For MongoDB
        private set;
    }

    /// <summary>
    /// Логин должен быть уникальным в системе. Логин решено не делать идентификатором, чтобы у пользователей была возможность в будущем поменять логин.
    /// </summary>
    public string Login { get; set; }
    public string LastName { get; set; }
    public string FirstName { get; set; }
        
    /// <summary>
    /// Количество сыгранных игр
    /// </summary>
    public int GamesPlayed { get; set; }
        
    /// <summary>
    /// Идентификатор игры, в которой этот пользователь участвует.
    /// Нужен, чтобы искать игру по первичному индексу, а не по полю Games.Players.UserId. В частности, чтобы не создавать дополнительный индекс на Games.Players.UserId
    /// </summary>
    public Guid? CurrentGameId { get; set; } // Для того, чтобы использовать индекс по Game.Id, а не искать игру по индексу на Game.Players.UserId

    public override string ToString()
    {
        return $"{nameof(Id)}: {Id}, {nameof(Login)}: {Login}, {nameof(CurrentGameId)}: {CurrentGameId}";
    }

    public void ExitGame()
    {
        if (CurrentGameId.HasValue)
        {
            GamesPlayed++;
            CurrentGameId = null;
        }
    }
}