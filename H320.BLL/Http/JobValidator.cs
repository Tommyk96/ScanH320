using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BoxAgr.BLL.Http
{
    public class JobValidator
    {
        public static List<ValidationResult> Validate(object model)
        {
            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, validationContext, validationResults, true);
            return validationResults;
        }
    }
}
