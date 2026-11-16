import { Component } from '@angular/core';
import { TranslatePipe } from '../../../../i18n/translate.pipe';

@Component({
  imports: [TranslatePipe],
  selector: 'app-offers',
  styleUrl: './offers.css',
  templateUrl: './offers.html',
})
export class Offers {}
