import { Component, OnInit } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { AbstractControl, AsyncValidator, AsyncValidatorFn, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

import { environment } from './../../environments/environment';
import { Country } from './country';

@Component({
  selector: 'app-country-edit',
  templateUrl: './country-edit.component.html',
  styleUrl: './country-edit.component.scss'
})
export class CountryEditComponent implements OnInit {

  // The view title.
  title?: string;

  // The form model.
  form!: FormGroup;

  // The country object to edit or create.
  country?: Country;

  // The country object id, as fetched from the active route:
  // It's NULL when adding a new country, and not NULL when editing an existing one.
  id?: number;

  // The countries array for the select element.
  countries?: Country[];

  constructor(private fb: FormBuilder, private activatedRoute: ActivatedRoute,
    private router: Router, private http: HttpClient) {
  }

  ngOnInit() {
    this.form = this.fb.group({
      name: ['', Validators.required, this.isDupeField("name")],
      iso2: ['', [Validators.required, Validators.pattern(/^[a-zA-Z]{2}$/)], this.isDupeField("iso2")],
      iso3: ['', [Validators.required, Validators.pattern(/^[a-zA-Z]{3}$/)], this.isDupeField("iso3")],
    });

    this.loadData();
  }

  loadData() {
    // Retrieve the ID from the 'id' parameter.
    var idParam = this.activatedRoute.snapshot.paramMap.get('id');
    this.id = idParam ? +idParam : 0;
    if (this.id) {
      // EDIT MODE
      // Fetch the country from the server.
      var url = `${environment.baseUrl}api/countries/${this.id}`;
      this.http.get<Country>(url).subscribe({
        next: (result) => {
          this.country = result;
          this.title = `Edit - ${this.country.name}`;

          // Update the form with the country value.
          this.form.patchValue(this.country);
        },
        error: (error) => console.error(error)
      });
    }
    else {
      // ADD NEW MODE
      this.title = "Create a new Country";
    }
  }

  onSubmit() {
    var country = (this.id) ? this.country : <Country>{};
    if (country) {
      country.name = this.form.controls['name'].value;
      country.iso2 = this.form.controls['iso2'].value;
      country.iso3 = this.form.controls['iso3'].value;

      if (this.id) {
        // EDIT MODE
        var url = `${environment.baseUrl}api/countries/${this.id}`;
        this.http.put<Country>(url, country).subscribe({
          next: (result) => {
            console.log(`Country ${country!.id} has been updated.`);

            // Go back to countries view.
            this.router.navigate(['/countries']);
          },
          error: (error) => console.error(error)
        });
      }
      else {
        // ADD NEW MODE
        var url = `${environment.baseUrl}api/countries`;
        this.http.post<Country>(url, country).subscribe({
          next: (result) => {
            console.log(`Country ${result.id} has been created.`);

            // Go back to countries view.
            this.router.navigate(['/countries']);
          },
          error: (error) => console.error(error)
        });
      }
    }
  }

  isDupeField(fieldName: string): AsyncValidatorFn {
    return (control: AbstractControl): Observable<{ [key: string]: any } | null> => {
      var params = new HttpParams()
        .set("countryId", (this.id) ? this.id.toString() : "0")
        .set("fieldName", fieldName)
        .set("fieldValue", control.value);
      var url = `${environment.baseUrl}api/Countries/IsDupeField`;
      return this.http.post<boolean>(url, null, { params })
        .pipe(map(result => {
          return (result ? { isDupeField: true } : null);
        }));
    }
  }
}
