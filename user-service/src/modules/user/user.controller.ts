import {Controller, Get, Post, Body, Param, Query, HttpCode} from '@nestjs/common';
import { UserService } from './user.service';
import { CreateUserDto } from './dto/create-user.dto';
import {BaseFilter} from "../filter/base.filter";
import {ApiTags} from "@nestjs/swagger";

@ApiTags('Users')
@Controller('users')
export class UserController {
    constructor(private readonly userService: UserService) {}

    @Post()
    @HttpCode(201)
    create(@Body() request: CreateUserDto) {
        return this.userService.create(request);
    }

    @Get()
    @HttpCode(200)
    findAll(@Query() filter: Partial<BaseFilter>) {
        return this.userService.findAll(filter);
    }

    @Get(':id')   
    @HttpCode(200)
    findOne(@Param('id') id: string) {
        return this.userService.findOne(id);
    }
}
