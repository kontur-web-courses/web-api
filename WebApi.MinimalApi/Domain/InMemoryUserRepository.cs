namespace WebApi.MinimalApi.Domain;

public class InMemoryUserRepository : IUserRepository
{
    private readonly Guid adminId = Guid.Parse("77777777-7777-7777-7777-777777777777");
    private const string AdminLogin = "Admin";
    private readonly Dictionary<Guid, UserEntity> entities = new Dictionary<Guid, UserEntity>();

    public InMemoryUserRepository()
    {
        AddAdmin();
    }

    private void AddAdmin()
    {
        var user = new UserEntity(adminId, AdminLogin, "Halliday", "James", 999, null);
        entities[user.Id] = user;
    }

    public UserEntity Insert(UserEntity user)
    {
        if (user.Id != Guid.Empty)
            throw new InvalidOperationException();

        var id = Guid.NewGuid();
        var entity = Clone(id, user);
        entities[id] = entity;
        return Clone(id, entity);
    }

    public UserEntity? FindById(Guid id)
    {
        return entities.TryGetValue(id, out var entity) ? Clone(id, entity) : null;
    }

    public UserEntity GetOrCreateByLogin(string login)
    {
        var existedUser = entities.Values.FirstOrDefault(u => u.Login == login);
        if (existedUser != null)
            return Clone(existedUser.Id, existedUser);

        var user = new UserEntity {Login = login};
        var entity = Clone(Guid.NewGuid(), user);
        entities[entity.Id] = entity;
        return Clone(entity.Id, entity);
    }

    public void Update(UserEntity user)
    {
        if (!entities.ContainsKey(user.Id))
            return;

        entities[user.Id] = Clone(user.Id, user);
    }

    public void UpdateOrInsert(UserEntity user, out bool isInserted)
    {
        if (user.Id == Guid.Empty)
            throw new InvalidOperationException();

        var id = user.Id;
        if (entities.ContainsKey(id))
        {
            entities[id] = Clone(id, user);
            isInserted = false;
            return;
        }

        var entity = Clone(id, user);
        entities[id] = entity;
        isInserted = true;
    }

    public void Delete(Guid id)
    {
        entities.Remove(id);
    }

    public PageList<UserEntity> GetPage(int pageNumber, int pageSize)
    {
        var count = entities.Count;
        var items = entities.Values
            .OrderBy(u => u.Login)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => Clone(u.Id, u))
            .ToList();
        return new PageList<UserEntity>(items, count, pageNumber, pageSize);
    }

    private UserEntity Clone(Guid id, UserEntity user)
    {
        return new UserEntity(id, user.Login, user.LastName, user.FirstName, user.GamesPlayed, user.CurrentGameId);
    }
}