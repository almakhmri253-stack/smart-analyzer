using SmartAnalyzer.ViewModels;

namespace SmartAnalyzer.Services;

public interface IAnalysisService
{
    Task<AnalysisResultViewModel> GetFileAnalysisAsync(int fileId, string userId);
    Task<ComparisonResultViewModel> GetComparisonAsync(ComparisonRequestViewModel request, string userId);
}
