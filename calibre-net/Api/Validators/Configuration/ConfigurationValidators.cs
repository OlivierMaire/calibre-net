using calibre_net.Shared.Contracts;
using FastEndpoints;
using FluentValidation;

namespace calibre_net.Api.Validators;

public class GetConfigurationValueValidator : Validator<GetConfigurationValueRequest>
{
    public GetConfigurationValueValidator()
    {
        RuleFor(x => x.Value)
            .NotEmpty()
            .WithMessage("Value can't be empty");
    }
}