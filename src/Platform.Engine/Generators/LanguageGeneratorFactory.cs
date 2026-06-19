using System;
using System.Collections.Generic;
using System.Linq;
using Platform.Engine.Models;

namespace Platform.Engine.Generators;

/// <summary>
/// Factory to resolve the appropriate language-specific code generator.
/// </summary>
public class LanguageGeneratorFactory
{
    private readonly IEnumerable<ILanguageGenerator> _generators;

    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageGeneratorFactory"/> class.
    /// </summary>
    /// <param name="generators">Registered language generators.</param>
    public LanguageGeneratorFactory(IEnumerable<ILanguageGenerator> generators)
    {
        _generators = generators;
    }

    /// <summary>
    /// Returns the generator matching the specified target language.
    /// </summary>
    /// <param name="language">The target language.</param>
    /// <returns>The generator instance.</returns>
    public ILanguageGenerator GetGenerator(TargetLanguage language)
    {
        var generator = _generators.FirstOrDefault(g => g.Language == language);
        if (generator == null)
        {
            throw new NotSupportedException($"Backend generation for {language} is not supported yet.");
        }
        return generator;
    }
}
