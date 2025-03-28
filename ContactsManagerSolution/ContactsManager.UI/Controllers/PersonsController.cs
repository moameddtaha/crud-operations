﻿using CRUDExample.Filters;
using CRUDExample.Filters.ActionFilters;
using CRUDExample.Filters.AuthorizationFilters;
using CRUDExample.Filters.ExceptionFilters;
using CRUDExample.Filters.ResourceFilters;
using CRUDExample.Filters.ResultFilters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Rotativa.AspNetCore;
using ServiceContracts;
using ServiceContracts.DTO;
using ServiceContracts.Enums;
using Services;

namespace CRUDExample.Controllers
{
    [Route("[controller]")]

    //[TypeFilter(typeof(ResponseHeaderActionFilter), Arguments = new object[] { "my-key-from-controller", "my-value-from-controller", 3 }, Order = 3)]

    [ResponseHeaderFilterFactory("my-key-from-controller", "my-value-from-controller", 3)]
    [TypeFilter(typeof(HandleExceptionFilter))]
    [TypeFilter(typeof(PersonsAlwaysRunResultFilter))]
    public class PersonsController : Controller
    {
        // Private fields
        private readonly IPersonsGetterService _personsGetterService;
        private readonly IPersonsAdderService _personsAdderService;
        private readonly IPersonsDeleterService _personsDeleterService;
        private readonly IPersonsUpdaterService _personsUpdaterService;
        private readonly IPersonsSorterService _personsSorterService;

        private readonly ICountriesAdderService _countriesAdderService;
        private readonly ICountriesGetterService _countriesGetterService;
        private readonly ICountriesUploaderService _countriesUploaderService;

        private readonly ILogger<PersonsController> _logger;

        // Constructor
        public PersonsController(IPersonsGetterService personsGetterService, IPersonsAdderService personsAdderService, IPersonsDeleterService personsDeleterService, IPersonsUpdaterService personsUpdaterService, IPersonsSorterService personsSorterService, ICountriesAdderService countriesAdderService, ICountriesGetterService countriesGetterService, ICountriesUploaderService countriesUploaderService, ILogger<PersonsController> logger)
        {
            _personsGetterService = personsGetterService;
            _personsAdderService = personsAdderService;
            _personsDeleterService = personsDeleterService;
            _personsUpdaterService = personsUpdaterService;
            _personsSorterService = personsSorterService;

            _countriesAdderService = countriesAdderService;
            _countriesGetterService = countriesGetterService;
            _countriesUploaderService = countriesUploaderService;

            _logger = logger;
        }

        // Url: /persons/index
        [Route("[action]")]
        [Route("/")]
        [TypeFilter(typeof(PersonsListActionFilter), Order = 4)]
        
        //[TypeFilter(typeof(ResponseHeaderActionFilter), Arguments = new object[] { "my-key-from-action", "my-value-from-action", 1 }, Order = 1)]

        [ResponseHeaderFilterFactory("my-key-from-action", "my-value-from-action", 1)]

        [ServiceFilter(typeof(PersonsListResultFilter))]
        [SkipFilter]
        public async Task<IActionResult> Index(string searchBy, string? searchString, string sortBy = nameof(PersonResponse.PersonName), SortOrderOptions sortOrder = SortOrderOptions.ASC)
        {
            _logger.LogInformation("Index action method of PersonsController");

            _logger.LogDebug($"searchBy: {searchBy}, searchString: {searchString}, searchBy: {sortBy}, sortOrder: {sortOrder}");

            // Search
            var persons = await _personsGetterService.GetFilteredPersons(searchBy, searchString);

            // Sort
            var sortedPersons = await _personsSorterService.GetSortedPersons(persons, sortBy, sortOrder);

            return View(sortedPersons); //Views/Persons/Index.cshtml
        }

        // Executes when the user clicks on "Create Person" hyperlink (while opening the create view)
        // Url: /persons/create
        [Route("[action]")]
        [HttpGet]
        //[ResponseHeaderActionFilter("my-key-from-action", "my-value-from-action", 4)]
        [ResponseHeaderFilterFactory("my-key-from-action", "my-value-from-action", 4)]

        public async Task<IActionResult> Create()
        {
            ViewBag.Countries = await GetCountrySelectListAsync();

            return View();
        }

        // Url: /persons/create
        [Route("[action]")]
        [HttpPost]
        [TypeFilter(typeof(PersonCreateAndEditPostActionFilter))]
        [TypeFilter(typeof(FeatureDisabledResourceFilter), Arguments = new object[] { false })]
        public async Task<IActionResult> Create(PersonAddRequest personRequest)
        {
            // Call the service method
            var personResponse = await _personsAdderService.AddPerson(personRequest);

            // Navigate to Index() action method (it makes another get request to "persons/index")
            return RedirectToAction("index", "Persons");
        }

        // Url: /persons/edit/1
        [Route("[action]/{personID}")]
        [HttpGet]
        [TypeFilter(typeof(TokenResultFilter))]
        public async Task<IActionResult> Edit(Guid personID)
        {
            PersonResponse? personResponse = await _personsGetterService.GetPersonbyPersonID(personID);
            if (personResponse == null)
            {
                return RedirectToAction("index", "Persons");
            }
            PersonUpdateRequest personUpdateRequest = personResponse.ToPersonUpdateRequest();
            ViewBag.Countries = await GetCountrySelectListAsync();

            return View(personUpdateRequest);
        }

        [Route("[action]/{personID}")]
        [HttpPost]
        [TypeFilter(typeof(PersonCreateAndEditPostActionFilter))]
        [TypeFilter(typeof(TokenAuthorizationFilter))]
        [TypeFilter(typeof(PersonsAlwaysRunResultFilter))]
        public async Task<IActionResult> Edit(PersonUpdateRequest personRequest)
        {
            PersonResponse? personResponse = await _personsGetterService.GetPersonbyPersonID(personRequest.PersonID);

            if (personResponse == null)
            {
                return RedirectToAction("index", "Persons");
            }

            PersonResponse updatedPerson = await _personsUpdaterService.UpdatePerson(personRequest);
            return RedirectToAction("Index");
        }


        [Route("[action]/{personID}")]
        [HttpGet]
        public async Task<IActionResult> Delete(Guid? personID)
        {
            PersonResponse? personResponse = await _personsGetterService.GetPersonbyPersonID(personID);
            if (personResponse == null)
            {
                return RedirectToAction("index", "Persons");
            }

            return View(personResponse);
        }

        [Route("[action]/{personID}")]
        [HttpPost]
        public async Task<IActionResult> Delete(PersonUpdateRequest personUpdateResult)
        {
            PersonResponse? personResponse = await _personsGetterService.GetPersonbyPersonID(personUpdateResult.PersonID);

            if (personResponse == null)
            {
                return RedirectToAction("index", "Persons");
            }

            await _personsDeleterService.DeletePerson(personResponse.PersonID);

            return RedirectToAction("index", "Persons");
        }

        [Route("[action]")]
        public async Task<IActionResult> PersonsPDF()
        {
            //Get list of Persons

            List<PersonResponse> persons = await _personsGetterService.GetAllPersons();

            // Return view as pdf
            return new ViewAsPdf("PersonsPDF", persons, ViewData)
            {
                PageMargins = new Rotativa.AspNetCore.Options.Margins()
                {
                    Top = 20,
                    Right = 20,
                    Bottom = 20,
                    Left = 20
                },
                PageOrientation = Rotativa.AspNetCore.Options.Orientation.Landscape
            };
        }

        [Route("[action]")]
        public async Task<IActionResult> PersonsCSV()
        {
            MemoryStream memoryStream = await _personsGetterService.GetPersonsCSV();

            return File(memoryStream, "application/octet-stream", "persons.csv");

        }

        [Route("[action]")]
        public async Task<IActionResult> PersonsExcel()
        {
            MemoryStream memoryStream = await _personsGetterService.GetPersonsExcel();

            return File(memoryStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "persons.xlsx");

        }

        #region Helper Functions

        private async Task<IEnumerable<SelectListItem>> GetCountrySelectListAsync()
        {
            var countries = await _countriesGetterService.GetAllCountries();
            return countries.Select(temp => new SelectListItem()
            {
                Text = temp.CountryName,
                Value = temp.CountryId.ToString()
            });
        }

        #endregion

    }
}
