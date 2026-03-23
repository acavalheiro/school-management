using Domain.Common;

namespace Domain.ValueObjects;

public record Address(string Street, string City, string PostalCode, string Country)
{
    public static Result<Address> Create(string street, string city, string postalCode, string country)
    {
        if (string.IsNullOrWhiteSpace(street))
            return Error.Validation(nameof(Street), "Street is required.");

        if (string.IsNullOrWhiteSpace(city))
            return Error.Validation(nameof(City), "City is required.");

        if (string.IsNullOrWhiteSpace(postalCode))
            return Error.Validation(nameof(PostalCode), "PostalCode is required.");

        if (string.IsNullOrWhiteSpace(country))
            return Error.Validation(nameof(Country), "Country is required.");

        return Result<Address>.Success(new Address(street, city, postalCode, country));
    }
}
