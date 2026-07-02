using PromptForge.Abstractions;
using PromptForge.Core.Metadata;
using PromptForge.Core.Metadata.Registry;

namespace PromptForge.Core.Builders;

public class PromptBuilderFactory(IPromptCompiler compiler)
{
    public PromptBuilder<TIn, TOut> Create<TIn, TOut>() where TIn : notnull where TOut : notnull
    {
        TypeMetadataRegistry.RegisterClrType(typeof(TIn));
        TypeMetadataRegistry.RegisterClrType(typeof(TOut));

        var scopeBuilder = new MetadataScopeBuilder(
            TypeMetadataRegistry.RegisterClrType(typeof(TIn)).TypeOccurrences
                .Concat(TypeMetadataRegistry.RegisterClrType(typeof(TOut)).TypeOccurrences)
        );
        return new PromptBuilder<TIn, TOut>(compiler, scopeBuilder);
    }
}