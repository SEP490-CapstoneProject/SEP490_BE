using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Data;

namespace Portfolio.Infrastructure.Repositories;

public class BlockRepository : IBlockRepository
{
    private readonly PortfolioDbContext _ctx;
    public BlockRepository(PortfolioDbContext ctx) => _ctx = ctx;

    public async Task<PortfolioBlock?> GetByIdWithDataAsync(int blockId)
        => await _ctx.PortfolioBlocks
            .Include(b => b.BlockType)
            .Include(b => b.Intro)
            .Include(b => b.Skills)
            .Include(b => b.Educations)
            .Include(b => b.Diplomas)
            .Include(b => b.Experiences)
            .Include(b => b.Projects).ThenInclude(p => p.Links)
            .Include(b => b.Awards)
            .Include(b => b.Activities)
            .Include(b => b.OtherInfos)
            .Include(b => b.References)
            .FirstOrDefaultAsync(b => b.Id == blockId);

    public async Task<List<PortfolioBlock>> GetByPortfolioIdAsync(int portfolioId)
        => await _ctx.PortfolioBlocks
            .Where(b => b.PortfolioId == portfolioId && b.IsVisible)
            .Include(b => b.BlockType)
            .Include(b => b.Intro)
            .Include(b => b.Skills)
            .Include(b => b.Educations)
            .Include(b => b.Diplomas)
            .Include(b => b.Experiences)
            .Include(b => b.Projects).ThenInclude(p => p.Links)
            .Include(b => b.Awards)
            .Include(b => b.Activities)
            .Include(b => b.OtherInfos)
            .Include(b => b.References)
            .OrderBy(b => b.DisplayOrder)
            .ToListAsync();

    public async Task<int> GetMaxOrderAsync(int portfolioId)
    {
        var max = await _ctx.PortfolioBlocks
            .Where(b => b.PortfolioId == portfolioId)
            .MaxAsync(b => (int?)b.DisplayOrder);
        return max ?? 0;
    }

    public async Task<int> CountByTypeAsync(int portfolioId, int blockTypeId)
        => await _ctx.PortfolioBlocks
            .CountAsync(b => b.PortfolioId == portfolioId && b.BlockTypeId == blockTypeId);

    public async Task<PortfolioBlock> CreateAsync(PortfolioBlock block)
    {
        _ctx.PortfolioBlocks.Add(block);
        await _ctx.SaveChangesAsync();
        return block;
    }

    public async Task<PortfolioBlock> UpdateAsync(PortfolioBlock block)
    {
        _ctx.PortfolioBlocks.Update(block);
        await _ctx.SaveChangesAsync();
        return block;
    }

    public async Task DeleteAsync(PortfolioBlock block)
    {
        _ctx.PortfolioBlocks.Remove(block);
        await _ctx.SaveChangesAsync();
    }

    public async Task ReorderAsync(List<(int blockId, int order)> reorders)
    {
        foreach (var (blockId, order) in reorders)
        {
            var block = await _ctx.PortfolioBlocks.FindAsync(blockId);
            if (block != null) block.DisplayOrder = order;
        }
        await _ctx.SaveChangesAsync();
    }

    public async Task AddIntroAsync(Intro intro) { _ctx.Intros.Add(intro); await _ctx.SaveChangesAsync(); }
    public async Task UpdateIntroAsync(Intro intro) { _ctx.Intros.Update(intro); await _ctx.SaveChangesAsync(); }

    public async Task AddSkillsAsync(IEnumerable<Skill> skills) { _ctx.Skills.AddRange(skills); await _ctx.SaveChangesAsync(); }
    public async Task RemoveSkillsAsync(int portfolioBlockId)
    {
        var skills = await _ctx.Skills.Where(s => s.PortfolioBlockId == portfolioBlockId).ToListAsync();
        _ctx.Skills.RemoveRange(skills);
        await _ctx.SaveChangesAsync();
    }

    public async Task AddEducationAsync(Education education) { _ctx.Educations.Add(education); await _ctx.SaveChangesAsync(); }
    public async Task UpdateEducationAsync(Education education) { _ctx.Educations.Update(education); await _ctx.SaveChangesAsync(); }

    public async Task AddDiplomaAsync(Diploma diploma) { _ctx.Diplomas.Add(diploma); await _ctx.SaveChangesAsync(); }
    public async Task UpdateDiplomaAsync(Diploma diploma) { _ctx.Diplomas.Update(diploma); await _ctx.SaveChangesAsync(); }

    public async Task AddExperienceAsync(Experience experience) { _ctx.Experiences.Add(experience); await _ctx.SaveChangesAsync(); }
    public async Task UpdateExperienceAsync(Experience experience) { _ctx.Experiences.Update(experience); await _ctx.SaveChangesAsync(); }

    public async Task AddProjectAsync(Project project) { _ctx.Projects.Add(project); await _ctx.SaveChangesAsync(); }
    public async Task UpdateProjectAsync(Project project) { _ctx.Projects.Update(project); await _ctx.SaveChangesAsync(); }

    public async Task AddAwardAsync(Award award) { _ctx.Awards.Add(award); await _ctx.SaveChangesAsync(); }
    public async Task UpdateAwardAsync(Award award) { _ctx.Awards.Update(award); await _ctx.SaveChangesAsync(); }

    public async Task AddActivitiesAsync(Activities activities) { _ctx.Activities.Add(activities); await _ctx.SaveChangesAsync(); }
    public async Task UpdateActivitiesAsync(Activities activities) { _ctx.Activities.Update(activities); await _ctx.SaveChangesAsync(); }

    public async Task AddOtherInfoAsync(OtherInfo otherInfo) { _ctx.OtherInfos.Add(otherInfo); await _ctx.SaveChangesAsync(); }
    public async Task UpdateOtherInfoAsync(OtherInfo otherInfo) { _ctx.OtherInfos.Update(otherInfo); await _ctx.SaveChangesAsync(); }

    public async Task AddReferenceAsync(Reference reference) { _ctx.References.Add(reference); await _ctx.SaveChangesAsync(); }
    public async Task UpdateReferenceAsync(Reference reference) { _ctx.References.Update(reference); await _ctx.SaveChangesAsync(); }
}
