import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { AccountService } from '../_services/account.service';
import { ToastrService } from 'ngx-toastr';
import { AbstractControl, FormBuilder, FormControl, FormGroup, ValidatorFn, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from 'src/environments/environment';
import { Country, City } from 'country-state-city';


@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent implements OnInit {
  @Output() cancelRegister =new EventEmitter();
  registerForm: FormGroup =new FormGroup({});
  maxDate: Date=new Date();
  validationErrors: string[] |undefined;
  isProcessingPassport = false;
  benchmarkMetrics: any = null;
  countriesList: any[] = [];
  citiesList: any[] = [];

  constructor(private accountService: AccountService, private toastr: ToastrService, 
    private fb: FormBuilder, private router: Router,private http: HttpClient) { }

  ngOnInit(): void {
    this.initializeForm();
    this.maxDate.setFullYear(this.maxDate.getFullYear() -18);
    this.countriesList = Country.getAllCountries();
  }

  initializeForm(){
    this.registerForm=this.fb.group({
      gender: ['male'],
      username: ['', Validators.required],
      knownAs: ['', Validators.required],
      dateOfBirth: ['', Validators.required],
      city: ['', Validators.required],
      country: ['', Validators.required],
      password: ['', [Validators.required,
        Validators.minLength(4),Validators.maxLength(8)]],
      confirmPassword:['',[Validators.required, this.matchValues("password")]],
    });
    this.registerForm.controls['password'].valueChanges.subscribe({
      next: ()=> this.registerForm.controls['confirmPassword'].updateValueAndValidity()
    })
    this.registerForm.get('country')?.valueChanges.subscribe({
    next: (countryCode) => {
      const cityControl = this.registerForm.get('city');
      cityControl?.reset();

      if (countryCode) {
        this.citiesList = City.getCitiesOfCountry(countryCode) || [];
        cityControl?.enable();
      } else {
        this.citiesList = [];
        cityControl?.disable();
      }
    }
  });
  }

  matchValues(matchTo: string):ValidatorFn{
    return (control: AbstractControl)=>{
      return control.value === control.parent?.get(matchTo)?.value ? null: {notMatching: true}
    }
  }

  register(){
    const dob=this.getDateOnly(this.registerForm.controls['dateOfBirth'].value);
    const values={...this.registerForm.value, dateOfBirth: dob}
  this.accountService.register(values).subscribe({
    next: () =>{
      this.router.navigateByUrl('/members')
    },
    error: error=>{
    this.validationErrors=error
    
    }
  })
  }
  cancel(){
    this.cancelRegister.emit(false);
  }

  private getDateOnly(dob: string | undefined){
    if(!dob) return;
    let theDob=new Date(dob);
    return new Date(theDob.setMinutes(theDob.getMinutes()-theDob.getTimezoneOffset()))
      .toISOString().slice(0,10);
  }
 onPassportUpload(event: any) {
  const file = event.target.files[0];
  if (!file) return;

  this.isProcessingPassport = true;
  this.benchmarkMetrics = null;

  const formData = new FormData();
  formData.append('file', file);

  // Using your production-ready environment.apiUrl syntax cleanly
  this.http.post<any>(environment.apiUrl + 'passport/process-passport', formData).subscribe({
    next: response => {
      this.isProcessingPassport = false;
      this.benchmarkMetrics = response.metrics;

      // 1. CRUCIAL: Set the country ISO code first so your reactive listener loads the cities list
      if (response.country) {
        this.registerForm.get('country')?.setValue(response.country.toUpperCase());
      }

      // 2. Set the rest of the text values safely
      if (response.username) this.registerForm.get('username')?.setValue(response.username);
      if (response.knownAs) this.registerForm.get('knownAs')?.setValue(response.knownAs);
      if (response.gender) this.registerForm.get('gender')?.setValue(response.gender.toLowerCase());

      // 3. Set the city. Since Gemini runs its own normalization, it will match your loaded dropdown options perfectly!
      if (response.city) {
        this.registerForm.get('city')?.setValue(response.city);
      }

      if (response.dateOfBirth) {
        const dobDate = new Date(response.dateOfBirth);
        if (!isNaN(dobDate.getTime())) {
          this.registerForm.get('dateOfBirth')?.setValue(dobDate);
        }
      }

      if (!response.isBarcodeValid) {
        this.toastr.warning('Passport data extracted, but validation zone appears cropped, missing, or obscured!');
      } else {
        this.toastr.success('Passport verified and autofilled successfully!');
      }
    },
    error: err => {
      this.isProcessingPassport = false;
      this.toastr.error(err.error || 'Failed to analyze passport image.');
    }
  });
}
}
