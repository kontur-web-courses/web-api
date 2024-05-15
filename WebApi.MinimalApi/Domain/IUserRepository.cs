namespace WebApi.MinimalApi.Domain;

public interface IUserRepository
{
    UserEntity Insert(UserEntity user);
    UserEntity? FindById(Guid id);
    UserEntity GetOrCreateByLogin(string login);
    void Update(UserEntity user);
    void UpdateOrInsert(UserEntity user, out bool isInserted);
    void Delete(Guid id);
    PageList<UserEntity> GetPage(int pageNumber, int pageSize);
}