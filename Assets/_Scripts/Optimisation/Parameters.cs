using GeneticSharp.Domain.Chromosomes;
using GeneticSharp.Domain.Randomizations;

public class MyChromosome : ChromosomeBase
{
    public MyChromosome() : base(2) // number of genes
    {
        CreateGenes();
    }

    public override Gene GenerateGene(int geneIndex)
    {
        return new Gene(RandomizationProvider.Current.GetDouble(0, 1)); // Example gene value
    }

    public override IChromosome CreateNew()
    {
        return new MyChromosome();
    }
}
