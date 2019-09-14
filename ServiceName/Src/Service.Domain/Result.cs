using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Service.Domain
{
    public class Result<T>
    {
        public T Value { get; set; }
        public List<ValidationResult> Validations { get; set; } = new List<ValidationResult>();
        public bool Success => !Validations.Any();

        /// <summary>
        /// Returns an instance of the builder to start the fluent creation of the object.
        /// </summary>
        public static Result<T> New()
        {
            return new Result<T>();
        }

        public Result<T> WithValidation(string validation)
        {
            Validations.Add(new ValidationResult(validation));
            return this;
        }
        public Result<T> WithValidations(params string[] validation)
        {
            Validations.AddRange(validation.Select(v => new ValidationResult(v)));
            return this;
        }
        public Result<T> WithValue(T result)
        {
            Value = result;
            return this;
        }
    }
}
