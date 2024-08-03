using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Randomizations;

public class Parameters : ChromosomeBase
{
    public Parameters() : base(3) // number of genes
    {
        CreateGenes();
    }

    public override Gene GenerateGene(int geneIndex)
    {
        return new Gene(RandomizationProvider.Current.GetDouble(0, 1)); // Example gene value
    }

    public override IChromosome CreateNew()
    {
        return new Parameters();
    }
}
