using SmartAnalyzer.ViewModels;

namespace SmartAnalyzer.Services;

public interface IFilterService
{
    Task<FilterResultViewModel> ApplyFiltersAsync(FilterRequestViewModel request, string userId);
    Task<int> SaveFilterAsync(FilterRequestViewModel request, string userId);
    Task<List<SavedFilterViewModel>> GetSavedFiltersAsync(int fileId, string userId);
    Task DeleteSavedFilterAsync(int filterId, string userId);
    Task<List<EntityCardViewModel>> GetEntityCardsAsync(EntityCardsRequestViewModel request, string userId);
    Task<CrossTabViewModel> GetCrossTabAsync(CrossTabRequestViewModel request, string userId);
}
