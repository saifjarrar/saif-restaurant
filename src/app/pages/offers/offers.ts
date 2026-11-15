import { TranslatePipe } from '../../i18n/translate.pipe';
import { Component } from '@angular/core';

@Component({
  imports: [TranslatePipe],
  selector: 'app-offers',
  styleUrl: './offers.css',
  templateUrl: './offers.html',
})
export class Offers {}
